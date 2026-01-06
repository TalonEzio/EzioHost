using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EzioHost.WebApp.Client.Extensions;

public static class AuthenticationStateExtension
{
    private const string UPLOAD_MAX_SPEED_MB_CLAIM = "UploadMaxSpeedMb";
    private const string STORAGE_CLAIM = "Storage";
    private const string USER_ID_CLAIM = ClaimTypes.NameIdentifier;

    extension(AuthenticationState authenticationState)
    {
        public Guid UserId => authenticationState.GetClaimValue<Guid>(USER_ID_CLAIM);

        public long UploadMaxSpeedMb =>
            authenticationState.GetClaimsValue<long>(UPLOAD_MAX_SPEED_MB_CLAIM)
                .DefaultIfEmpty(0)
                .Max();

        public long Storage =>
            authenticationState.GetClaimsValue<long>(STORAGE_CLAIM)
                .DefaultIfEmpty(0)
                .Max();

        public T GetClaimValue<T>(string claimType) where T : struct, IParsable<T>
        {
            if (authenticationState.User is { Identity.IsAuthenticated: false }) return default;
            var claimValue = authenticationState.User.Claims
                .FirstOrDefault(x => x.Type == claimType)
                ?.Value;

            var tryGetValue = T.TryParse(claimValue, null, out var result);
            return tryGetValue ? result : default;
        }

        public IEnumerable<T> GetClaimsValue<T>(string claimType) where T : struct, IParsable<T>
        {
            if (authenticationState.User is { Identity.IsAuthenticated: false }) yield break;
            var claimValues = authenticationState.User.Claims.Where(x => x.Type == claimType);

            foreach (var claim in claimValues)
            {
                var tryGetValue = T.TryParse(claim.Value, null, out var result);
                if (tryGetValue) yield return result;
            }
        }
    }
}