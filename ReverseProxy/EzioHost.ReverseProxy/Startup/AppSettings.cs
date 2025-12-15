using System.Security.Claims;

namespace EzioHost.ReverseProxy.Startup;

public class AppSettings
{
    public OpenIdConnectSetting OpenIdConnect { get; set; } = new();
    public GarnetCacheSetting GarnetCache { get; set; } = new();
}

public class OpenIdConnectSetting
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WebApiScope { get; set; } = string.Empty;
    public string UserNameClaimType { get; set; } = "preferred_username";
    public string NameClaimType { get; set; } = ClaimTypes.Name;
    public string RoleClaimType { get; set; } = ClaimTypes.Role;
}

public class GarnetCacheSetting
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 6379;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Database { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public int DefaultExpiryDays { get; set; }
}