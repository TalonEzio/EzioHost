using EzioHost.Domain.Enums;

namespace EzioHost.Domain.Entities
{
    public class FileUpload : BaseCreatedEntityWithUserId<Guid>
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public long UploadedBytes { get; set; } = 0;
        public string? Checksum { get; set; }

        public VideoEnum.FileUploadStatus Status { get; set; } = VideoEnum.FileUploadStatus.Pending;
        public bool IsCompleted => FileSize == UploadedBytes;
    }
}
