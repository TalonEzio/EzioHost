using System.Security.Claims;

namespace EzioHost.ReverseProxy
{
    public class AppSettings
    {
        public OpenIdConnectSetting OpenIdConnect { get; set; } = new OpenIdConnectSetting();
    }

    public class OpenIdConnectSetting
    {
        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string WebApiScope { get; set; } = string.Empty;
        public string AdminUserName { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public string NameClaimType { get; set; } = ClaimTypes.Name;
        public string RoleClaimType { get; set; } = ClaimTypes.Role;
    }
}
