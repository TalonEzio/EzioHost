using System.Security.Claims;

namespace EzioHost.ReverseProxy.Startup;

public class AppSettings
{
    public OpenIdConnectSetting OpenIdConnect { get; set; } = new();
    public UrlSettings Urls { get; set; } = new();
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

public class UrlSettings
{
    public string WebApi { get; set; } = string.Empty;
}