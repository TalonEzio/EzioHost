using System.Security.Claims;

namespace EzioHost.WebAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {

        public static Guid GetUserId(this ClaimsPrincipal user, string claimTypes)
        {
            if (user.Identity?.IsAuthenticated != true)
                return Guid.Empty;

            var userId = user.Claims.FirstOrDefault(x => x.Type == claimTypes)?.Value;
            var parse = Guid.TryParse(userId, out var result);
            return parse ? result : Guid.Empty;
        }

        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            return GetUserId(user, ClaimTypes.NameIdentifier);
        }

        public static string GetUserIdString(this ClaimsPrincipal user)
        {
            return GetUserId(user, ClaimTypes.NameIdentifier).ToString();
        }
    }
}
