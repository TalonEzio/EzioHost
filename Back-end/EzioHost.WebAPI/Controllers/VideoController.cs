using System.Security.Claims;
using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController(IDirectoryProvider directoryProvider, IVideoService videoService, IMapper mapper) : ControllerBase
    {
        private string UploadFolder => directoryProvider.GetBaseUploadFolder();

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetVideos()
        {
            var userId = User.GetUserId();
            var videos = (await videoService.GetVideos(x => x.CreatedBy == userId, [x => x.VideoStreams])).ToList();

            var videoDtos = mapper.Map<List<VideoDto>>(videos);

            
            return Ok(videoDtos);
        }

        [HttpGet("play/{videoId:guid}")]
        public async Task<IActionResult> GetVideoById([FromRoute] Guid videoId)
        {
            var video = await videoService.GetVideoById(videoId);
            if (video == null || !System.IO.File.Exists(video.M3U8Location))
            {
                return NotFound();
            }

            var fileContent = await System.IO.File.ReadAllBytesAsync(video.M3U8Location);
            return File(fileContent, "application/x-mpegURL");
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadVideoChunk([FromForm] IFormFile file, [FromForm] long fileSize)
        {
            var tempFilePath = Path.Combine(UploadFolder, file.FileName + ".part");

            await using (var stream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                await file.CopyToAsync(stream);
            }

            var fileInfo = new FileInfo(tempFilePath);

            if (fileInfo.Length >= fileSize)
            {
                string finalPath = Path.Combine(UploadFolder, file.FileName);
                System.IO.File.Move(tempFilePath, finalPath, true);

            }

            return Created();
        }

        [HttpGet("DRM/{videoStreamId:guid}")]
        public async Task<IActionResult> GetDrm([FromRoute] Guid videoStreamId)
        {
            var video = await videoService.GetVideoByVideoStreamId(videoStreamId);

            if (video == null)
            {
                return NotFound();
            }

            var contentKey = video.VideoStreams.First(x => x.Id == videoStreamId).Key;
            var userIdString = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            switch (video.ShareType)
            {
                case VideoEnum.VideoShareType.Public:
                case VideoEnum.VideoShareType.Internal when User.Identity is { IsAuthenticated: true }:
                    return Content(contentKey);

                case VideoEnum.VideoShareType.Internal:
                    return BadRequest();

                case VideoEnum.VideoShareType.Private when User.Identity is { IsAuthenticated: true }:
                    var parse = Guid.TryParse(userIdString, out var userId);
                    if (!parse) return BadRequest();
                    if (video.CreatedBy == userId)
                    {
                        return Content(contentKey);
                    }
                    break;
            }

            return BadRequest();
        }

    }
}