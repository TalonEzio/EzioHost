using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using EzioHost.Domain.Settings;
using EzioHost.Shared.Extensions;
using EzioHost.WebAPI.Startup;
using Microsoft.Extensions.Options;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VideoController(
    IVideoService videoService,
    IMapper mapper,
    IUpscaleService upscaleService,
    IOnnxModelService modelService,
    IDirectoryProvider directoryProvider,
    IOptions<AppSettings> appSettings) : ControllerBase
{
    private string WebRootPath => directoryProvider.GetWebRootPath();

    private StorageSettings StorageSettings => appSettings.Value.Storage;
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetVideos(
        int pageNumber = 1,
        int pageSize = 10,
        bool? includeStreams = null,
        bool? includeUpscaled = false)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var userId = User.UserId;
            if (userId == Guid.Empty)
                return Unauthorized("User ID not found");

            Expression<Func<Video, object>>[]? includes;

            if (includeStreams == true && includeUpscaled == true)
                includes = [x => x.VideoStreams, x => x.VideoUpscales];
            else if (includeStreams == true)
                includes = [x => x.VideoStreams];
            //else if (includeUpscaled == true)
            //    includes = [x => x.VideoUpscales];
            else
                includes = [];

            var videos = (await videoService.GetVideos(
                pageNumber,
                pageSize,
                x => x.CreatedBy == userId,
                includes)).ToList();

            var videoDtos = mapper.Map<List<VideoDto>>(videos);

            return Ok(videoDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, $"Internal server error occurred while retrieving videos.");
        }
    }
    [HttpGet("download/{videoId:guid}")]
    [Authorize]
    public async Task<IActionResult> DownloadVideo([FromRoute] Guid videoId)
    {
        try
        {
            if (videoId == Guid.Empty)
                return BadRequest("Invalid video ID");

            var video = await videoService.GetVideoById(videoId);
            if (video == null)
                return NotFound("Video not found");

            if (video.Status != VideoEnum.VideoStatus.Ready)
                return BadRequest("Video is not ready for download");

            var fileLocation = Path.Combine(WebRootPath, video.RawLocation);
            if (!System.IO.File.Exists(fileLocation))
                return NotFound("Video file not found");

            var fileContent = await System.IO.File.ReadAllBytesAsync(fileLocation);
            return File(fileContent, "application/octet-stream", $"{video.Title}");
        }
        catch (FileNotFoundException)
        {
            return NotFound("Video file not found");
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Access denied");
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error occurred while downloading video");
        }
    }

    [HttpGet("download-upscale/{videoId:guid}")]
    [Authorize]
    public async Task<IActionResult> DownloadVideoUpscale([FromRoute] Guid videoId)
    {
        var video = await videoService.GetVideoWithReadyUpscale(videoId);
        if (video == null || !video.VideoUpscales.Any()) return NotFound();
        if (video.Status != VideoEnum.VideoStatus.Ready) return BadRequest();

        var videoUpscale = video.VideoUpscales.First();
        var fileLocation = Path.Combine(WebRootPath, videoUpscale.OutputLocation);

        var fileContent = await System.IO.File.ReadAllBytesAsync(fileLocation);
        return File(fileContent, "application/octet-stream", $"{Path.GetFileName(videoUpscale.OutputLocation)}");
    }


    [HttpGet("{videoId:guid}")]
    public async Task<IActionResult> GetVideoById([FromRoute] Guid videoId)
    {
        var video = await videoService.GetVideoById(videoId);
        if (video == null) return NotFound();

        if (video.Status != VideoEnum.VideoStatus.Ready) return BadRequest();

        var videoDto = mapper.Map<VideoDto>(video);

        switch (video.ShareType)
        {
            case VideoEnum.VideoShareType.Public:
            case VideoEnum.VideoShareType.Internal when User.Identity is { IsAuthenticated: true }:
                return Ok(videoDto);

            case VideoEnum.VideoShareType.Internal:
                break;

            case VideoEnum.VideoShareType.Private when User.Identity is { IsAuthenticated: true }:
                if (video.CreatedBy == User.UserId) return Ok(videoDto);

                break;
        }

        return BadRequest();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpdateVideo([FromBody] VideoUpdateDto videoUpdateDto)
    {
        var video = await videoService.GetVideoById(videoUpdateDto.Id);
        if (video == null) return NotFound();

        try
        {
            video.Title = videoUpdateDto.Title;
            video.ShareType = videoUpdateDto.ShareType;
            video.ModifiedBy = User.UserId;

            await videoService.UpdateVideo(video);

            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{videoId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteVideo([FromRoute] Guid videoId)
    {
        var video = await videoService.GetVideoById(videoId);
        if (video == null) return NotFound();

        try
        {
            await videoService.DeleteVideo(video);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }


    [HttpGet("DRM/{videoStreamId:guid}")]
    public async Task<IActionResult> GetDrm([FromRoute] Guid videoStreamId)
    {
        var video = await videoService.GetVideoByVideoStreamId(videoStreamId);

        if (video == null) return NotFound();

        var contentKey = video.VideoStreams.First(x => x.Id == videoStreamId).Key;

        switch (video.ShareType)
        {
            case VideoEnum.VideoShareType.Public:
            case VideoEnum.VideoShareType.Internal when User.Identity is { IsAuthenticated: true }:
                return Content(contentKey);

            case VideoEnum.VideoShareType.Internal:
                break;
            case VideoEnum.VideoShareType.Private when User.Identity is { IsAuthenticated: true }:
                if (video.CreatedBy == User.UserId) return Content(contentKey);
                break;
        }

        return BadRequest();
    }

    [HttpGet("stream/manifest/{videoId}/{resolution}")]
    public async Task<IActionResult> GetManifest(Guid videoId, int resolution)
    {
        if (!Enum.IsDefined(typeof(VideoEnum.VideoResolution), resolution))
            return BadRequest("Resolution không hợp lệ");

        var resEnum = (VideoEnum.VideoResolution)resolution;

        var video = await videoService.GetVideoById(videoId);
        if (video == null) return NotFound("Không tìm thấy video");

        var targetVideoStream = video.VideoStreams.FirstOrDefault(x => x.Resolution == resEnum);
        if (targetVideoStream == null || string.IsNullOrEmpty(targetVideoStream.M3U8Location))
            return NotFound("Không tìm thấy stream");


        var m3U8LocalLocation = Uri.UnescapeDataString(targetVideoStream.M3U8Location);
        var physicalPath = Path.Combine(WebRootPath, m3U8LocalLocation);

        if (!System.IO.File.Exists(physicalPath))
            return NotFound("File manifest gốc chưa được tạo");

        string content = await System.IO.File.ReadAllTextAsync(physicalPath);

        string cdnBaseUrl = $"{StorageSettings.PublicDomain.TrimEnd('/')}/videos/{videoId}/{resolution}/";

        var resolutionDescription = resEnum.GetDescription();
        content = content.Replace(resolutionDescription, $"{cdnBaseUrl}{resolutionDescription}");

        return Content(content, "application/vnd.apple.mpegurl");
    }

    [HttpPost("{videoId:guid}/upscale/{modelId:guid}")]
    public async Task<IActionResult> UpscaleVideo([FromRoute] Guid videoId, [FromRoute] Guid modelId)
    {
        var video = await videoService.GetVideoById(videoId);
        var model = await modelService.GetOnnxModelById(modelId);

        if (video == null || model == null) return NotFound();

        if (video.Status != VideoEnum.VideoStatus.Ready) return BadRequest();

        var videoUpscale = new VideoUpscale
        {
            Scale = model.Scale,
            Video = video,
            Model = model,
            Status = VideoEnum.VideoUpscaleStatus.Queue,
            CreatedBy = HttpContext.User.UserId
        };

        await upscaleService.AddNewVideoUpscale(videoUpscale);

        return Created();
    }

    [HttpGet("statistics")]
    [Authorize]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var userId = User.UserId;
            if (userId == Guid.Empty)
                return Unauthorized("User ID not found");

            // Get all videos for the user (with a large page size to get all)
            // Include VideoStreams and VideoUpscales to calculate storage
            var videos = (await videoService.GetVideos(
                1,
                10000, // Large page size to get all videos
                x => x.CreatedBy == userId,
                [x => x.VideoStreams, x => x.VideoUpscales])).ToList();

            var totalVideos = videos.Count;
            var readyVideos = videos.Count(v => v.Status == VideoEnum.VideoStatus.Ready);
            long totalStorageUsed = 0;

            // Calculate total storage used
            foreach (var video in videos)
            {
                // Calculate storage for raw video file
                if (!string.IsNullOrEmpty(video.RawLocation))
                {
                    var rawFilePath = Path.Combine(WebRootPath, video.RawLocation);
                    if (System.IO.File.Exists(rawFilePath))
                    {
                        var fileInfo = new FileInfo(rawFilePath);
                        totalStorageUsed += fileInfo.Length;
                    }
                }

                // Calculate storage for video streams (HLS segments)
                if (video.VideoStreams != null)
                {
                    foreach (var stream in video.VideoStreams)
                    {
                        if (!string.IsNullOrEmpty(stream.M3U8Location))
                        {
                            var m3U8Path = Path.Combine(WebRootPath, Uri.UnescapeDataString(stream.M3U8Location));
                            var streamDirectory = Path.GetDirectoryName(m3U8Path);
                            if (Directory.Exists(streamDirectory))
                            {
                                var tsFiles = Directory.GetFiles(streamDirectory, "*.ts");
                                foreach (var tsFile in tsFiles)
                                {
                                    var tsFileInfo = new FileInfo(tsFile);
                                    totalStorageUsed += tsFileInfo.Length;
                                }
                                
                                // Also include m3u8 file
                                if (System.IO.File.Exists(m3U8Path))
                                {
                                    var m3U8FileInfo = new FileInfo(m3U8Path);
                                    totalStorageUsed += m3U8FileInfo.Length;
                                }
                            }
                        }
                    }
                }

                // Calculate storage for upscaled videos
                if (video.VideoUpscales != null)
                {
                    foreach (var upscale in video.VideoUpscales)
                    {
                        if (!string.IsNullOrEmpty(upscale.OutputLocation))
                        {
                            var upscaleFilePath = Path.Combine(WebRootPath, upscale.OutputLocation);
                            if (System.IO.File.Exists(upscaleFilePath))
                            {
                                var upscaleFileInfo = new FileInfo(upscaleFilePath);
                                totalStorageUsed += upscaleFileInfo.Length;
                            }
                        }
                    }
                }

                // Calculate storage for thumbnails
                if (!string.IsNullOrEmpty(video.Thumbnail))
                {
                    var thumbnailPath = Path.Combine(WebRootPath, video.Thumbnail);
                    if (System.IO.File.Exists(thumbnailPath))
                    {
                        var thumbnailFileInfo = new FileInfo(thumbnailPath);
                        totalStorageUsed += thumbnailFileInfo.Length;
                    }
                }
            }

            var statistics = new VideoStatisticsDto
            {
                TotalVideos = totalVideos,
                ReadyVideos = readyVideos,
                TotalStorageUsedBytes = totalStorageUsed
            };

            return Ok(statistics);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error occurred while retrieving statistics: {ex.Message}");
        }
    }

    [HttpGet("statistics/detailed")]
    [Authorize]
    public async Task<IActionResult> GetDetailedStatistics()
    {
        try
        {
            var userId = User.UserId;
            if (userId == Guid.Empty)
                return Unauthorized("User ID not found");

            // Get all videos for the user
            var videos = (await videoService.GetVideos(
                1,
                10000,
                x => x.CreatedBy == userId,
                [x => x.VideoStreams, x => x.VideoUpscales])).ToList();

            if (!videos.Any())
            {
                return Ok(new VideoDetailedStatisticsDto());
            }

            // Determine time grouping (day, week, or month)
            var oldestVideo = videos.Min(v => v.CreatedAt);
            var newestVideo = videos.Max(v => v.CreatedAt);
            var daysDiff = (newestVideo - oldestVideo).TotalDays;
            
            string dateFormat;
            Func<DateTime, DateTime> groupByFunc;
            
            if (daysDiff <= 30)
            {
                // Group by day
                dateFormat = "dd/MM/yyyy";
                groupByFunc = d => new DateTime(d.Year, d.Month, d.Day);
            }
            else if (daysDiff <= 180)
            {
                // Group by week
                dateFormat = "dd/MM/yyyy";
                groupByFunc = d =>
                {
                    var startOfWeek = d.AddDays(-(int)d.DayOfWeek);
                    return new DateTime(startOfWeek.Year, startOfWeek.Month, startOfWeek.Day);
                };
            }
            else
            {
                // Group by month
                dateFormat = "MM/yyyy";
                groupByFunc = d => new DateTime(d.Year, d.Month, 1);
            }

            // Build video timeline (cumulative count)
            var videoTimeline = videos
                .GroupBy(v => groupByFunc(v.CreatedAt))
                .OrderBy(g => g.Key)
                .Select((g, index) => new VideoTimeSeriesDto
                {
                    Date = g.Key.ToString(dateFormat),
                    Count = videos.Count(v => groupByFunc(v.CreatedAt) <= g.Key)
                })
                .ToList();

            // Build storage timeline (cumulative storage)
            var storageTimeline = new List<VideoTimeSeriesDto>();
            long cumulativeStorage = 0;
            
            foreach (var dateGroup in videos
                .GroupBy(v => groupByFunc(v.CreatedAt))
                .OrderBy(g => g.Key))
            {
                // Calculate storage for videos created on this date
                foreach (var video in dateGroup)
                {
                    cumulativeStorage += CalculateVideoStorage(video);
                }

                storageTimeline.Add(new VideoTimeSeriesDto
                {
                    Date = dateGroup.Key.ToString(dateFormat),
                    Count = (int)cumulativeStorage,
                    StorageBytes = cumulativeStorage
                });
            }

            // Build resolution distribution
            var resolutionDistribution = videos
                .GroupBy(v => v.Resolution)
                .Select(g => new VideoDistributionDto
                {
                    Label = g.Key.GetDescription(),
                    Count = g.Count(),
                    Percentage = videos.Count > 0 ? Math.Round((double)g.Count() / videos.Count * 100, 1) : 0
                })
                .OrderByDescending(d => d.Count)
                .ToList();

            // Build status distribution
            var statusDistribution = videos
                .GroupBy(v => v.Status)
                .Select(g => new VideoDistributionDto
                {
                    Label = g.Key.GetDescription(),
                    Count = g.Count(),
                    Percentage = videos.Count > 0 ? Math.Round((double)g.Count() / videos.Count * 100, 1) : 0
                })
                .OrderByDescending(d => d.Count)
                .ToList();

            var detailedStatistics = new VideoDetailedStatisticsDto
            {
                VideoTimeline = videoTimeline,
                StorageTimeline = storageTimeline,
                ResolutionDistribution = resolutionDistribution,
                StatusDistribution = statusDistribution
            };

            return Ok(detailedStatistics);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error occurred while retrieving detailed statistics: {ex.Message}");
        }
    }

    private long CalculateVideoStorage(Domain.Entities.Video video)
    {
        long storage = 0;

        // Calculate storage for raw video file
        if (!string.IsNullOrEmpty(video.RawLocation))
        {
            var rawFilePath = Path.Combine(WebRootPath, video.RawLocation);
            if (System.IO.File.Exists(rawFilePath))
            {
                var fileInfo = new FileInfo(rawFilePath);
                storage += fileInfo.Length;
            }
        }

        // Calculate storage for video streams (HLS segments)
        if (video.VideoStreams != null)
        {
            foreach (var stream in video.VideoStreams)
            {
                if (!string.IsNullOrEmpty(stream.M3U8Location))
                {
                    var m3U8Path = Path.Combine(WebRootPath, Uri.UnescapeDataString(stream.M3U8Location));
                    var streamDirectory = Path.GetDirectoryName(m3U8Path);
                    if (Directory.Exists(streamDirectory))
                    {
                        var tsFiles = Directory.GetFiles(streamDirectory, "*.ts");
                        foreach (var tsFile in tsFiles)
                        {
                            var tsFileInfo = new FileInfo(tsFile);
                            storage += tsFileInfo.Length;
                        }
                        
                        // Also include m3u8 file
                        if (System.IO.File.Exists(m3U8Path))
                        {
                            var m3U8FileInfo = new FileInfo(m3U8Path);
                            storage += m3U8FileInfo.Length;
                        }
                    }
                }
            }
        }

        // Calculate storage for upscaled videos
        if (video.VideoUpscales != null)
        {
            foreach (var upscale in video.VideoUpscales)
            {
                if (!string.IsNullOrEmpty(upscale.OutputLocation))
                {
                    var upscaleFilePath = Path.Combine(WebRootPath, upscale.OutputLocation);
                    if (System.IO.File.Exists(upscaleFilePath))
                    {
                        var upscaleFileInfo = new FileInfo(upscaleFilePath);
                        storage += upscaleFileInfo.Length;
                    }
                }
            }
        }

        // Calculate storage for thumbnails
        if (!string.IsNullOrEmpty(video.Thumbnail))
        {
            var thumbnailPath = Path.Combine(WebRootPath, video.Thumbnail);
            if (System.IO.File.Exists(thumbnailPath))
            {
                var thumbnailFileInfo = new FileInfo(thumbnailPath);
                storage += thumbnailFileInfo.Length;
            }
        }

        return storage;
    }
}