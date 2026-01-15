using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models;

public class SubtitleTranscribeSettingDto
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
    public WhisperEnum.WhisperModelType ModelType { get; set; }
    public bool UseGpu { get; set; }
    public int? GpuDeviceId { get; set; }
}

public class SubtitleTranscribeSettingUpdateDto
{
    public bool IsEnabled { get; set; }
    public WhisperEnum.WhisperModelType ModelType { get; set; }
    public bool UseGpu { get; set; }
    public int? GpuDeviceId { get; set; }
}
