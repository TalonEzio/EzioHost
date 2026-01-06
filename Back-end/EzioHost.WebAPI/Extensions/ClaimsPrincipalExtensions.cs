using System.Security.Claims;

namespace EzioHost.WebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid UserId => user.GetUserId(ClaimTypes.NameIdentifier);

        public Guid GetUserId(string claimTypes)
        {
            if (user.Identity?.IsAuthenticated != true)
                return Guid.Empty;

            var userId = user.Claims.FirstOrDefault(x => x.Type == claimTypes)?.Value;
            var canGetUserId = Guid.TryParse(userId, out var result);
            return canGetUserId ? result : Guid.Empty;
        }
    }
}