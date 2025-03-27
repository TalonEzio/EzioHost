using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Domain.Enums;

namespace EzioHost.Domain.Entities
{
    [Table("VideoStreams")]
    public class VideoStream
    {
        public Guid Id { get; set; }

        public string M3U8Location { get; set; } = string.Empty;

        public VideoEnum.VideoResolution Resolution { get; set; }
        public Video Video { get; set; } = new();

        [ForeignKey(nameof(Video))]
        public Guid VideoId { get; set; }
    }
}
