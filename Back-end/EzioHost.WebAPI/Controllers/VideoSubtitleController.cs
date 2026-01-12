using AutoMapper;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class VideoSubtitleController(
    IVideoSubtitleService videoSubtitleService,
    IVideoService videoService,
    IMapper mapper) : ControllerBase
{
    [HttpPost("{videoId:guid}")]
    public async Task<IActionResult> UploadSubtitle(
        [FromRoute] Guid videoId,
        [FromForm] string language,
        [FromForm] IFormFile? file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File không được để trống");
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                return BadRequest("Tên ngôn ngữ không được để trống");
            }

            var userId = HttpContext.User.UserId;
            if (userId == Guid.Empty)
            {
                return Unauthorized("User ID not found");
            }

            // Check if user owns the video
            var video = await videoService.GetVideoById(videoId);
            if (video == null)
            {
                return NotFound("Video không tồn tại");
            }

            if (video.CreatedBy != userId)
            {
                return Forbid("Bạn không có quyền upload subtitle cho video này");
            }

            await using var fileStream = file.OpenReadStream();
            var subtitle = await videoSubtitleService.UploadSubtitleAsync(
                videoId,
                language,
                fileStream,
                file.FileName,
                file.Length,
                userId);

            var subtitleDto = mapper.Map<VideoSubtitleDto>(subtitle);
            return Created(string.Empty, subtitleDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi upload subtitle: {ex.Message}");
        }
    }

    [HttpGet("{videoId:guid}")]
    public async Task<IActionResult> GetSubtitles([FromRoute] Guid videoId)
    {
        try
        {
            var subtitles = await videoSubtitleService.GetSubtitlesByVideoIdAsync(videoId);
            var subtitleDtos = mapper.Map<IEnumerable<VideoSubtitleDto>>(subtitles);
            return Ok(subtitleDtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi lấy danh sách subtitle: {ex.Message}");
        }
    }

    [HttpGet("file/{subtitleId:guid}")]
    public async Task<IActionResult> GetSubtitleFile([FromRoute] Guid subtitleId)
    {
        try
        {
            var subtitle = await videoSubtitleService.GetSubtitleByIdAsync(subtitleId);
            if (subtitle == null)
            {
                return NotFound("Subtitle không tồn tại");
            }

            // Check video access permission
            var video = await videoService.GetVideoById(subtitle.VideoId);
            if (video == null)
            {
                return NotFound("Video không tồn tại");
            }

            var userId = HttpContext.User.UserId;
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            // Check share type permissions
            switch (video.ShareType)
            {
                case Shared.Enums.VideoEnum.VideoShareType.Public:
                    break; // Allow access
                case Shared.Enums.VideoEnum.VideoShareType.Internal when isAuthenticated:
                    break; // Allow access
                case Shared.Enums.VideoEnum.VideoShareType.Private when isAuthenticated && video.CreatedBy == userId:
                    break; // Allow access
                default:
                    return Forbid("Bạn không có quyền truy cập subtitle này");
            }

            var fileStream = await videoSubtitleService.GetSubtitleFileStreamAsync(subtitleId);
            return File(fileStream, "text/vtt", subtitle.FileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi lấy file subtitle: {ex.Message}");
        }
    }

    [HttpDelete("{subtitleId:guid}")]
    public async Task<IActionResult> DeleteSubtitle([FromRoute] Guid subtitleId)
    {
        try
        {
            var userId = HttpContext.User.UserId;
            if (userId == Guid.Empty)
            {
                return Unauthorized("User ID not found");
            }

            var subtitle = await videoSubtitleService.GetSubtitleByIdAsync(subtitleId);
            if (subtitle == null)
            {
                return NotFound("Subtitle không tồn tại");
            }

            // Check if user owns the video
            var video = await videoService.GetVideoById(subtitle.VideoId);
            if (video == null)
            {
                return NotFound("Video không tồn tại");
            }

            if (video.CreatedBy != userId)
            {
                return Forbid("Bạn không có quyền xóa subtitle này");
            }

            await videoSubtitleService.DeleteSubtitleAsync(subtitleId);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi xóa subtitle: {ex.Message}");
        }
    }
}
