using System.Text;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Extensions;

namespace EzioHost.Shared.Models;

public class VideoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public VideoEnum.VideoResolution Resolution { get; set; }
    public VideoEnum.VideoStatus Status { get; set; }
    public VideoEnum.VideoShareType ShareType { get; set; } = VideoEnum.VideoShareType.Private;
    public DateTime CreatedAt { get; set; }

    public bool CanPlay => Status == VideoEnum.VideoStatus.Ready;

    public bool CanUpscale => CanPlay && Resolution <= VideoEnum.VideoResolution._480p &&
                              VideoStreams.All(x => x.Resolution != VideoEnum.VideoResolution.Upscaled);

    public List<VideoStreamDto> VideoStreams { get; set; } = [];
    public List<VideoUpscaleDto> VideoUpscales { get; set; } = [];
    public List<VideoSubtitleDto> Subtitles { get; set; } = [];

    public string PlayerJsMetadata
    {
        get
        {
            var builder = new StringBuilder();

            foreach (var stream in VideoStreams.OrderBy(x => x.Resolution))
                builder.Append($"[{stream.Resolution.GetDescription()}]{stream.M3U8Location},");
            return builder.ToString().TrimEnd(',');
        }
    }

    public string SubtitleMetadata
    {
        get
        {
            if (!Subtitles.Any())
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var subtitle in Subtitles)
                builder.Append($"[{subtitle.Language}]{subtitle.Url}?ext=.vtt,");
            return builder.ToString().TrimEnd(',');
        }
    }
}

public class VideoStreamDto
{
    public Guid Id { get; set; }

    public string M3U8Location { get; set; } = string.Empty;

    public VideoEnum.VideoResolution Resolution { get; set; }
}

public class VideoUpscaleDto
{
    public Guid Id { get; set; }
    public string OutputLocation { get; set; } = string.Empty;
    public int Scale { get; set; }
    public Guid ModelId { get; set; }
    public Guid VideoId { get; set; }
    public VideoEnum.VideoResolution Resolution { get; set; }
    public VideoEnum.VideoUpscaleStatus Status { get; set; } = VideoEnum.VideoUpscaleStatus.Queue;
}