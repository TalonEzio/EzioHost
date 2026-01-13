using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EncodingQualitySettingController(
    IEncodingQualitySettingService encodingQualitySettingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var userId = User.UserId;
            if (userId == Guid.Empty)
                return Unauthorized("User ID not found");

            var settings = await encodingQualitySettingService.GetUserSettings(userId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] EncodingQualitySettingUpdateRequest request)
    {
        try
        {
            var userId = User.UserId;
            if (userId == Guid.Empty)
                return Unauthorized("User ID not found");

            if (request == null || request.Settings == null || !request.Settings.Any())
                return BadRequest("Settings are required");

            // Validate: At least one setting must be enabled
            var enabledCount = request.Settings.Count(s => s.IsEnabled);
            if (enabledCount == 0)
                return BadRequest("At least one resolution must be enabled for encoding");

            var updatedSettings = await encodingQualitySettingService.UpdateUserSettings(userId, request);
            return Ok(updatedSettings);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}