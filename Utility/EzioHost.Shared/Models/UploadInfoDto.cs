using System.Text.Json.Serialization;

namespace EzioHost.Shared.Models;

public class UploadInfoDto
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public long ReceivedBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? Checksum { get; set; }
    public Guid UserId { get; set; }

    [JsonIgnore] public bool IsCompleted => FileSize == ReceivedBytes;
}