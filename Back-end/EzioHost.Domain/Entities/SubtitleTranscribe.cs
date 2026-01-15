using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

[Table("SubtitleTranscribes")]
public class SubtitleTranscribe : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(Video))] public Guid VideoId { get; set; }

    public Video Video { get; set; } = new();

    public string Language { get; set; } = string.Empty;

    public VideoEnum.SubtitleTranscribeStatus Status { get; set; } = VideoEnum.SubtitleTranscribeStatus.Queue;

    public string? ErrorMessage { get; set; }
}
