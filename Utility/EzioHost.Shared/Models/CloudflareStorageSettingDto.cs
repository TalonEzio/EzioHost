namespace EzioHost.Shared.Models;

public class CloudflareStorageSettingDto
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
}

public class CloudflareStorageSettingUpdateDto
{
    public bool IsEnabled { get; set; }
}
