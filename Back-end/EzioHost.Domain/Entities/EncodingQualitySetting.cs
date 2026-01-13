using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

[Table("EncodingQualitySettings")]
public class EncodingQualitySetting : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public VideoEnum.VideoResolution Resolution { get; set; }
    public int BitrateKbps { get; set; }
    public bool IsEnabled { get; set; } = true;
}