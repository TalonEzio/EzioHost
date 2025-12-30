using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Domain.Helpers;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

public class FileUpload : BaseCreatedEntityWithUserId<Guid>
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long ReceivedBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? Checksum { get; set; }

    public string PhysicalPath
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(PhysicalPath));
    } = string.Empty;

    public VideoEnum.FileUploadStatus Status { get; set; } = VideoEnum.FileUploadStatus.Pending;

    [NotMapped] public bool IsCompleted => Status == VideoEnum.FileUploadStatus.Completed && FileSize == ReceivedBytes;
}