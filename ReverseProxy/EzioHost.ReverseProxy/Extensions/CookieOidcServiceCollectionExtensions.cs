using EzioHost.ReverseProxy.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EzioHost.ReverseProxy.Extensions
{
    public static class CookieOidcServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureCookieOidcRefresh(this IServiceCollection services, string cookieScheme, string oidcScheme, TimeSpan refreshTimeSpan)
        {
            services.AddTransient<CookieOidcRefresher>();
            services.AddOptions<CookieAuthenticationOptions>(cookieScheme).Configure<CookieOidcRefresher>((cookieOptions, refresher) =>
            {
                cookieOptions.Events.OnValidatePrincipal = context => refresher.ValidateOrRefreshCookieAsync(context, oidcScheme, refreshTimeSpan);
            });
            services.AddOptions<OpenIdConnectOptions>(oidcScheme).Configure(oidcOptions =>
            {
                // Request a refresh_token.
                oidcOptions.Scope.Add(OpenIdConnectScope.OfflineAccess);
                // Store the refresh_token.
                oidcOptions.SaveTokens = true;
            });
            return services;
        }
    }
}
