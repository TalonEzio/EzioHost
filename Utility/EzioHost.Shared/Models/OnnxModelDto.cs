using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models;

public class OnnxModelDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    //public string FileLocation { get; set; } = string.Empty;
    public string DemoInput { get; set; } = string.Empty;
    public string DemoOutput { get; set; } = string.Empty;
    public int Scale { get; set; }
    public int MustInputWidth { get; set; }
    public int MustInputHeight { get; set; }
    public OnnxModelPrecision Precision { get; set; }
    public bool CanPreview => !string.IsNullOrEmpty(DemoInput) && !string.IsNullOrEmpty(DemoOutput);
}