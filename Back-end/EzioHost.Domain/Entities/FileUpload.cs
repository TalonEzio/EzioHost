using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

public class FileUpload : BaseCreatedEntityWithUserId<Guid>
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? Checksum { get; set; }
    public VideoEnum.FileUploadStatus Status { get; set; } = VideoEnum.FileUploadStatus.Pending;

    [NotMapped] public bool IsCompleted => Status == VideoEnum.FileUploadStatus.Completed;
}