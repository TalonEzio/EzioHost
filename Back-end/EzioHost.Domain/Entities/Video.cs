using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Domain.Enums;

namespace EzioHost.Domain.Entities
{
    [Table("Videos")]
    public class Video : BaseAuditableEntityWithUserId<Guid>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RawLocation { get; set; } = string.Empty;
        public string M3U8Location { get; set; } = string.Empty;
        public VideoEnum.VideoResolution Resolution { get; set; }
        public VideoEnum.VideoStatus Status { get; set; }
        public VideoEnum.VideoType Type { get; set; }
        public VideoEnum.VideoShareType ShareType { get; set; } = VideoEnum.VideoShareType.Private;
        public ICollection<VideoStream> VideoStreams { get; set; } = [];

        public void UpdateResolution(int height)
        {
            Resolution = height switch
            {
                >= 340 and <= 380 => VideoEnum.VideoResolution._360p,
                >= 460 and <= 500 => VideoEnum.VideoResolution._480p,
                >= 680 and <= 720 => VideoEnum.VideoResolution._720p,
                >= 1060 and <= 1100 => VideoEnum.VideoResolution._1080p,
                >= 1420 and <= 1460 => VideoEnum.VideoResolution._1440p,
                >= 2140 and <= 2180 => VideoEnum.VideoResolution._2160p,
                _ => VideoEnum.VideoResolution.Nothing
            };
        }

        [NotMapped] public bool IsReady => Status == VideoEnum.VideoStatus.Ready;

        [NotMapped] public bool IsValid => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(RawLocation);
    }


}
