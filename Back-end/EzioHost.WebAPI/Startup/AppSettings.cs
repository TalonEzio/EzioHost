using EzioHost.Domain.Settings;
using EzioHost.WebAPI.Settings;

namespace EzioHost.WebAPI.Startup;

public class AppSettings
{
    public JwtOidcSetting JwtOidc { get; set; } = new();
    public VideoEncodeSetting VideoEncode { get; set; } = new();
}