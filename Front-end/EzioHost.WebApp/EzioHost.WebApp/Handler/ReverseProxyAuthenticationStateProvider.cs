using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace EzioHost.WebApp.Handler
{
    public class ReverseProxyAuthenticationStateProvider(ILoggerFactory loggerFactory,NavigationManager navigation)
        : RevalidatingServerAuthenticationStateProvider(loggerFactory)
    {
        protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            //implement later
            navigation.Refresh();
            return Task.FromResult(true);
        }

        protected override TimeSpan RevalidationInterval { get; } = TimeSpan.FromMinutes(1);
    }
}
