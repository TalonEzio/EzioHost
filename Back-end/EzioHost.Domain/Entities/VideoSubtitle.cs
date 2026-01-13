using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Domain.Helpers;

namespace EzioHost.Domain.Entities;

[Table("VideoSubtitles")]
public class VideoSubtitle : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(Video))] public Guid VideoId { get; set; }

    public Video Video { get; set; } = new();

    public string Language { get; set; } = string.Empty;

    public string LocalPath
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(LocalPath));
    } = string.Empty;

    public string CloudPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }
}