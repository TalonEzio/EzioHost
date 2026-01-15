using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

[Table("WhisperSettings")]
public class SubtitleTranscribeSetting : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public WhisperEnum.WhisperModelType ModelType { get; set; } = WhisperEnum.WhisperModelType.Base;

    public bool UseGpu { get; set; } = false;

    public int? GpuDeviceId { get; set; }
}
