using System.Diagnostics;
using System.Security.Claims;
using AutoMapper;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController(
        IVideoService videoService,
        IMapper mapper,
        IUpscaleService upscaleService,
        IOnnxModelService modelService) : ControllerBase
    {

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetVideos()
        {
            var userId = User.GetUserId();
            var videos = (await videoService.GetVideos(x => x.CreatedBy == userId, [x => x.VideoStreams, x => x.VideoUpscales])).ToList();

            var videoDtos = mapper.Map<List<VideoDto>>(videos);


            return Ok(videoDtos);
        }

        [HttpGet("play/{videoId:guid}")]
        public async Task<IActionResult> GetM3U8ById([FromRoute] Guid videoId)
        {
            var video = await videoService.GetVideoById(videoId);
            if (video == null)
            {
                return NotFound();
            }

            if (video.Status != VideoEnum.VideoStatus.Ready)
            {
                return BadRequest();
            }

            var fileContent = await System.IO.File.ReadAllBytesAsync(video.M3U8Location);
            return File(fileContent, "application/x-mpegURL");
        }

        [HttpGet("{videoId:guid}")]
        public async Task<IActionResult> GetVideoById([FromRoute] Guid videoId)
        {
            var video = await videoService.GetVideoById(videoId);
            if (video == null)
            {
                return NotFound();
            }

            if (video.Status != VideoEnum.VideoStatus.Ready)
            {
                return BadRequest();
            }

            var videoDto = mapper.Map<VideoDto>(video);

            switch (video.ShareType)
            {
                case VideoEnum.VideoShareType.Public:
                case VideoEnum.VideoShareType.Internal when User.Identity is { IsAuthenticated: true }:
                    return Ok(videoDto);

                case VideoEnum.VideoShareType.Internal:
                    return BadRequest();

                case VideoEnum.VideoShareType.Private when User.Identity is { IsAuthenticated: true }:
                    if (video.CreatedBy == User.GetUserId())
                    {
                        return Ok(videoDto);
                    }

                    break;
            }

            return BadRequest();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateVideo([FromBody] VideoDto videoDto)
        {
            var video = await videoService.GetVideoById(videoDto.Id);
            if (video == null)
            {
                return NotFound();
            }

            try
            {
                video.Title = videoDto.Title;
                video.ShareType = videoDto.ShareType;
                video.Type = videoDto.Type;
                video.ModifiedBy = User.GetUserId();

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
            if (video == null)
            {
                return NotFound();
            }

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
                    if (video.CreatedBy == User.GetUserId())
                    {
                        return Content(contentKey);
                    }

                    break;
            }

            return BadRequest();
        }


        [HttpPost("{videoId:guid}/upscale/{modelId:guid}")]
        public async Task<IActionResult> UpscaleVideo([FromRoute] Guid videoId, [FromRoute] Guid modelId)
        {
            var video = await videoService.GetVideoById(videoId);
            var model = await modelService.GetOnnxModelById(modelId);

            if (video == null || model == null)
            {
                return NotFound();
            }

            if (video.Status != VideoEnum.VideoStatus.Ready)
            {
                return BadRequest();
            }

            var videoUpscale = new VideoUpscale()
            {
                Scale = model.Scale,
                Video = video,
                Model = model,
                Status = VideoEnum.VideoUpscaleStatus.Queue
            };

            await upscaleService.AddNewVideoUpscale(videoUpscale);

            return Created();
        }
    }
}