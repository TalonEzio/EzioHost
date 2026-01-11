using System.Linq.Expressions;
using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VideoController(
    IVideoService videoService,
    IMapper mapper,
    IUpscaleService upscaleService,
    IOnnxModelService modelService,
    IDirectoryProvider directoryProvider) : ControllerBase
{
    private string WebRootPath => directoryProvider.GetWebRootPath();

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
            else if (includeUpscaled == true)
                includes = [x => x.VideoUpscales];
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
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error occurred while retrieving videos");
        }
    }

    [HttpGet("play/{videoId:guid}")]
    public async Task<IActionResult> GetM3U8ById([FromRoute] Guid videoId)
    {
        try
        {
            if (videoId == Guid.Empty)
                return BadRequest("Invalid video ID");

            var video = await videoService.GetVideoById(videoId);
            if (video == null)
                return NotFound("Video not found");

            if (video.Status != VideoEnum.VideoStatus.Ready)
                return BadRequest("Video is not ready for playback");

            if (!System.IO.File.Exists(video.M3U8Location))
                return NotFound("Video file not found");

            var fileContent = await System.IO.File.ReadAllBytesAsync(video.M3U8Location);
            return File(fileContent, "application/x-mpegURL");
        }
        catch (FileNotFoundException ex)
        {
            return NotFound("Video file not found");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized("Access denied");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error occurred while retrieving video");
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
        catch (FileNotFoundException ex)
        {
            return NotFound("Video file not found");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized("Access denied");
        }
        catch (Exception ex)
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
                return BadRequest();

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
}