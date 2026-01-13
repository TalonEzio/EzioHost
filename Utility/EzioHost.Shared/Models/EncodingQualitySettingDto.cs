using EzioHost.Shared.Enums;

namespace EzioHost.Shared.Models;

public class EncodingQualitySettingDto
{
    public Guid Id { get; set; }
    public VideoEnum.VideoResolution Resolution { get; set; }
    public int BitrateKbps { get; set; }
    public bool IsEnabled { get; set; }
}

public class EncodingQualitySettingUpdateRequest
{
    public List<EncodingQualitySettingUpdateItem> Settings { get; set; } = [];
}

public class EncodingQualitySettingUpdateItem
{
    public Guid? Id { get; set; }
    public VideoEnum.VideoResolution Resolution { get; set; }
    public int BitrateKbps { get; set; }
    public bool IsEnabled { get; set; }
}