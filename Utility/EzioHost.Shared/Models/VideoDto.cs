using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models
{
    public class VideoDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string M3U8Location { get; set; } = string.Empty;
        public VideoEnum.VideoResolution Resolution { get; set; }
        public VideoEnum.VideoStatus Status { get; set; }
        public VideoEnum.VideoType Type { get; set; }
        public VideoEnum.VideoShareType ShareType { get; set; } = VideoEnum.VideoShareType.Private;
        public bool CanPlay => Status == VideoEnum.VideoStatus.Ready;
        public bool CanUpscale => CanPlay && Resolution <= VideoEnum.VideoResolution._480p && !VideoUpscales.Any();
        public ICollection<VideoStreamDto> VideoStreams { get; set; } = [];
        public ICollection<VideoUpscaleDto> VideoUpscales { get; set; } = [];
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
}
