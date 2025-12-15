using EzioHost.Shared.Private.Endpoints;
using EzioHost.WebApp.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;

namespace EzioHost.WebApp.Startup;

public static class AuthenticationStartup
{
    public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        // Add Authentication
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, ReverseProxyAuthenticationStateProvider>();

        builder.Services.AddAuthentication(cfg =>
            {
                cfg.DefaultScheme = ReverseProxyAuthenticationSchemeConstants.AuthenticationScheme;
                cfg.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                cfg.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            //custom oidc cookie handler
            .AddScheme<ReverseProxyAuthenticationSchemeOptions, ReverseProxyAuthenticationHandler>(
                ReverseProxyAuthenticationSchemeConstants.AuthenticationScheme,
                cfg => { cfg.ReverseProxyBaseUrl = BaseUrl.ReverseProxyUrl; });
        builder.Services.AddAuthorization();

        return builder;
    }
}