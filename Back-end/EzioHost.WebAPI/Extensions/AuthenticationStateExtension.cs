using System.Security.Claims;

namespace EzioHost.WebAPI.Extensions
{
    public static class AuthenticationStateExtension
    {

        public static Guid GetUserId(this ClaimsPrincipal user, string claimTypes)
        {
            if (user is { Identity.IsAuthenticated: false }) return Guid.Empty;

            var userId = user.Claims.FirstOrDefault(x => x.Type == claimTypes)?.Value;
            var parse = Guid.TryParse(userId, out var result);
            return parse ? result : Guid.Empty;
        }

        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            return GetUserId(user, ClaimTypes.NameIdentifier);
        }
    }
}
