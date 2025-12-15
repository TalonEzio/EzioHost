namespace EzioHost.WebAPI.Settings;

public class JwtOidcSetting
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string MetaDataAddress { get; set; } = string.Empty;
}