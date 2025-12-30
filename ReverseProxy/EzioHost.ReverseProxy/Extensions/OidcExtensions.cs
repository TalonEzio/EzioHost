using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EzioHost.ReverseProxy.Extensions;

public static class OidcExtensions
{
    public static Task<string?> GetDownstreamAccessTokenAsync(this HttpContext httpContext)
    {
        return httpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
    }
}