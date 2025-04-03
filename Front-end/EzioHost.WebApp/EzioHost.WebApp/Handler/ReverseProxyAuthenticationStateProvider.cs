using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace EzioHost.WebApp.Handler
{
    public class ReverseProxyAuthenticationStateProvider(ILoggerFactory loggerFactory)
        : RevalidatingServerAuthenticationStateProvider(loggerFactory)
    {
        protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            //implement later
            return Task.FromResult(true);
        }

        protected override TimeSpan RevalidationInterval { get; } = TimeSpan.FromMinutes(1);
    }
}
