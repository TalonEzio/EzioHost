using EzioHost.Domain.Settings;
using EzioHost.WebAPI.Settings;

namespace EzioHost.WebAPI.Startup;

public class AppSettings
{
    public JwtOidcSetting JwtOidc { get; set; } = new();
    public VideoEncodeSettings VideoEncode { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
}