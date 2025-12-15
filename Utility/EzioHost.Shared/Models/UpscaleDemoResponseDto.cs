namespace EzioHost.Shared.Models;

public class UpscaleDemoResponseDto
{
    public Guid ModelId { get; set; }
    public string DemoInput { get; set; } = string.Empty;
    public string DemoOutput { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
}