using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Enums;

namespace EzioHost.Core.Services.Implement;

public class VideoResolutionService(
    IEncodingQualitySettingService encodingQualitySettingService) : IVideoResolutionService
{
    public int GetBandwidthForResolution(string resolution)
    {
        return resolution switch
        {
            "360p" => 800000,
            "480p" => 1400000,
            "720p" => 2800000,
            "960p" => 4000000,
            "1080p" => 5000000,
            "1440p" => 8000000,
            "1920p" => 8000000,
            "2160p" => 15000000,
            "AI Upscaled" => 5000000,
            _ => 1000000
        };
    }

    public async Task<int> GetBandwidthForResolutionAsync(string resolution, Guid userId)
    {
        VideoEnum.VideoResolution resolutionEnum;
        bool parsed = false;

        // Try to parse resolution to enum
        if (resolution == "AI Upscaled")
        {
            resolutionEnum = VideoEnum.VideoResolution.Upscaled;
            parsed = true;
        }
        else
        {
            var cleanResolution = resolution.Replace("p", "").Replace(" ", "");
            parsed = Enum.TryParse<VideoEnum.VideoResolution>(cleanResolution, true, out resolutionEnum) ||
                     Enum.TryParse<VideoEnum.VideoResolution>($"_{cleanResolution}", true, out resolutionEnum);
        }

        if (!parsed)
        {
            // Fallback to default if resolution can't be parsed
            return GetBandwidthForResolution(resolution);
        }

        // Get user settings
        var activeSettings = await encodingQualitySettingService.GetActiveSettingsForEncoding(userId);
        
        // Find matching setting
        var setting = activeSettings.FirstOrDefault(s => s.Resolution == resolutionEnum);

        if (setting != null)
        {
            // Convert kbps to bps
            return setting.BitrateKbps * 1000;
        }

        // Fallback to default hardcoded value
        return GetBandwidthForResolution(resolution);
    }

    public string GetResolutionDimensions(string resolution)
    {
        return resolution switch
        {
            "360p" => "640x360",
            "480p" => "854x480",
            "720p" => "1280x720",
            "960p" => "1280x960",
            "1080p" => "1920x1080",
            "1440p" => "2560x1440",
            "1920p" => "2560x1920",
            "2160p" => "3840x2160",
            _ => "1920x1080"
        };
    }
}