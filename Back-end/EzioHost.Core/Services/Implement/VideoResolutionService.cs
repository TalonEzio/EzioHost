namespace EzioHost.Core.Services.Implement;

public class VideoResolutionService : IVideoResolutionService
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