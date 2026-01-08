using System.Text;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Extensions;

namespace EzioHost.Core.Services.Implement;

public class M3U8PlaylistService(IVideoResolutionService videoResolutionService) : IM3U8PlaylistService
{
    private const string M3_U8_HEADER = "#EXTM3U";
    private const string STREAM_INFO_PREFIX = "#EXT-X-STREAM-INF:";

    public async Task BuildFullPlaylistAsync(Video video, string absoluteM3U8Location)
    {
        var m3U8Folder = new FileInfo(absoluteM3U8Location).Directory!.FullName;
        if (!Directory.Exists(m3U8Folder)) Directory.CreateDirectory(m3U8Folder);

        await using var m3U8FileStream =
            new FileStream(absoluteM3U8Location, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        m3U8FileStream.SetLength(0); // Clear existing content

        var playlistContent = BuildPlaylistContent(video.VideoStreams);
        await m3U8FileStream.WriteAsync(Encoding.UTF8.GetBytes(playlistContent));
    }

    public async Task AppendStreamToPlaylistAsync(Video video, VideoStream videoStream, string absoluteM3U8Location)
    {
        var currentResolution = videoStream.Resolution.GetDescription();
        var filePath = Path.Combine(currentResolution, Path.GetFileName(videoStream.M3U8Location))
            .Replace("\\", "/");

        var streamEntry = BuildStreamEntry(currentResolution, filePath);

        await using var m3U8FileStream = new FileStream(
            absoluteM3U8Location,
            FileMode.OpenOrCreate,
            FileAccess.Write);
        m3U8FileStream.Seek(0, SeekOrigin.End);
        await m3U8FileStream.WriteAsync(Encoding.UTF8.GetBytes(streamEntry));
    }

    private string BuildPlaylistContent(ICollection<VideoStream> videoStreams)
    {
        var builder = new StringBuilder();
        builder.AppendLine(M3_U8_HEADER);

        foreach (var videoStream in videoStreams)
        {
            var currentResolution = videoStream.Resolution.GetDescription();
            var filePath = Path.Combine(currentResolution, Path.GetFileName(videoStream.M3U8Location))
                .Replace("\\", "/");

            builder.AppendLine(BuildStreamInfoLine(currentResolution, filePath));
            builder.AppendLine(filePath);
        }

        return builder.ToString();
    }

    private string BuildStreamEntry(string resolution, string filePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine(BuildStreamInfoLine(resolution, filePath));
        builder.AppendLine(filePath);
        return builder.ToString();
    }

    private string BuildStreamInfoLine(string resolution, string filePath)
    {
        var bandwidth = videoResolutionService.GetBandwidthForResolution(resolution);
        var dimensions = videoResolutionService.GetResolutionDimensions(resolution);
        return $"{STREAM_INFO_PREFIX}BANDWIDTH={bandwidth},RESOLUTION={dimensions}";
    }
}