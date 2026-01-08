namespace EzioHost.Core.Services.Interface;

public interface IVideoResolutionService
{
    /// <summary>
    ///     Gets the bandwidth (in bits per second) for a given resolution
    /// </summary>
    int GetBandwidthForResolution(string resolution);

    /// <summary>
    ///     Gets the dimensions string (e.g., "1920x1080") for a given resolution
    /// </summary>
    string GetResolutionDimensions(string resolution);
}