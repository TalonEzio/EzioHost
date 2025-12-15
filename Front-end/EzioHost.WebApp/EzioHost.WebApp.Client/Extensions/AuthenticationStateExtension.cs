using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EzioHost.WebApp.Client.Extensions;

public static class AuthenticationStateExtension
{
    extension(AuthenticationState authenticationState)
    {
        public Guid UserId => GetUserId(authenticationState, ClaimTypes.NameIdentifier);

        public Guid GetUserId(string claimTypes)
        {
            if (authenticationState.User is { Identity.IsAuthenticated: false }) return Guid.Empty;

            var userId = authenticationState.User.Claims.FirstOrDefault(x => x.Type == claimTypes)?.Value;
            var parse = Guid.TryParse(userId, out var result);
            return parse ? result : Guid.Empty;
        }
    }
}