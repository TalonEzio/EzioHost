namespace EzioHost.WebAPI
{
    public class AppSettings
    {
        public JwtOidcSetting JwtOidc { get; set; } = new();
    }

    public class JwtOidcSetting
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        public string MetaDataAddress { get; set; } = string.Empty;
    }
}
