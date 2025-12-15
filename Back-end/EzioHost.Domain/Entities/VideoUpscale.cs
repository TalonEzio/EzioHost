using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Domain.Helpers;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

[Table("VideoUpscales")]
public class VideoUpscale : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }

    public string OutputLocation
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(OutputLocation));
    } = string.Empty;

    public int Scale { get; set; }
    public Guid ModelId { get; set; }
    public OnnxModel Model { get; set; } = null!;
    public Guid VideoId { get; set; }
    public VideoEnum.VideoResolution Resolution { get; set; }
    public VideoEnum.VideoUpscaleStatus Status { get; set; } = VideoEnum.VideoUpscaleStatus.Queue;
    public Video Video { get; set; } = null!;
}