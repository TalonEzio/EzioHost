using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models;

public class OnnxModelMetadataDto
{
    public int? Scale { get; set; }
    public int? MustInputWidth { get; set; }
    public int? MustInputHeight { get; set; }
    public string? ErrorMessage { get; set; }

    public TensorElementType ElementType { get; set; }
}