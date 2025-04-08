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
        public bool CanUpscale => CanPlay &&  Resolution <= VideoEnum.VideoResolution._720p;
        public ICollection<VideoStreamDto> VideoStreams { get; set; } = [];
    }

    public class VideoStreamDto
    {
        public Guid Id { get; set; }

        public string M3U8Location { get; set; } = string.Empty;

        public VideoEnum.VideoResolution Resolution { get; set; }
    }
}
