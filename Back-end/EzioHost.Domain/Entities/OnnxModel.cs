using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Domain.Helpers;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

[Table("OnnxModels")]
public class OnnxModel : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileLocation { get; set; } = string.Empty;

    public string DemoInput
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(DemoInput));
    } = string.Empty;

    public string DemoOutput
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(DemoOutput));
    } = string.Empty;

    public int Scale { get; set; }
    public int MustInputWidth { get; set; }
    public int MustInputHeight { get; set; }
    public TensorElementType ElementType { get; set; }

    public ICollection<VideoUpscale> VideoUpscales { get; set; } = [];
}