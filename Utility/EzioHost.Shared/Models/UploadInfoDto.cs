using System.ComponentModel;
using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models
{
    public class UploadInfoDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public long UploadedBytes { get; set; } = 0;
        public string? Checksum { get; set; }
        public Guid UserId { get; set; }
        public VideoEnum.VideoType Type { get; set; } = VideoEnum.VideoType.Other;
        public bool IsCompleted => FileSize == UploadedBytes;
    }

}
