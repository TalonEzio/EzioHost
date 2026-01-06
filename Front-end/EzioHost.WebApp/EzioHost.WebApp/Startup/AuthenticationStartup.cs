using EzioHost.WebApp.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;

namespace EzioHost.WebApp.Startup;

public static class AuthenticationStartup
{
    public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder builder,
        AppSettings settings)
    {
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, ReverseProxyAuthenticationStateProvider>();

        builder.Services.AddAuthentication(cfg =>
            {
                cfg.DefaultScheme = ReverseProxyAuthenticationSchemeConstants.AuthenticationScheme;
                cfg.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                cfg.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddScheme<ReverseProxyAuthenticationSchemeOptions, ReverseProxyAuthenticationHandler>(
                ReverseProxyAuthenticationSchemeConstants.AuthenticationScheme,
                cfg => cfg.ReverseProxyBaseUrl = settings.Urls.ReverseProxy);
        builder.Services.AddAuthorization();

        return builder;
    }
}