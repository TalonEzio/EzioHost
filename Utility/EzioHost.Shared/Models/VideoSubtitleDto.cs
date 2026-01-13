namespace EzioHost.Shared.Models;

public class VideoSubtitleDto
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Url { get; set; } = string.Empty;
}