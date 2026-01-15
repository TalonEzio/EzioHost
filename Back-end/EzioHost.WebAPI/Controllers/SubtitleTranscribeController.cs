using EzioHost.Core.Services.Interface;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SubtitleTranscribeController(
    ISubtitleTranscribeService subtitleTranscribeService,
    IVideoService videoService) : ControllerBase
{
    [HttpPost("{videoId:guid}")]
    public async Task<IActionResult> CreateTranscribeRequest(
        [FromRoute] Guid videoId,
        [FromBody] CreateTranscribeRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Language))
                return BadRequest("Ngôn ngữ không được để trống");

            var userId = HttpContext.User.UserId;
            if (userId == Guid.Empty) return Unauthorized("User ID not found");

            // Check if user owns the video
            var video = await videoService.GetVideoById(videoId);
            if (video == null) return NotFound("Video không tồn tại");

            if (video.CreatedBy != userId)
                return Forbid("Bạn không có quyền transcribe video này");

            var transcribe = await subtitleTranscribeService.CreateTranscribeRequestAsync(
                videoId,
                request.Language,
                userId);

            return Created(string.Empty, new { transcribe.Id, transcribe.Status });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi tạo request transcribe: {ex.Message}");
        }
    }

    public class CreateTranscribeRequestDto
    {
        public string Language { get; set; } = string.Empty;
    }
}
