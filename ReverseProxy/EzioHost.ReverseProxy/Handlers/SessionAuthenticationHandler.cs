using EzioHost.ReverseProxy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EzioHost.ReverseProxy.Handlers
{
    public class SessionAuthenticationHandler(
        IOptionsMonitor<SessionAuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        ISessionCacheService sessionCacheService)
        : AuthenticationHandler<SessionAuthenticationSchemeOptions>(options, loggerFactory, encoder)
    {
        private readonly ILogger<SessionAuthenticationHandler> _logger = loggerFactory.CreateLogger<SessionAuthenticationHandler>();

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                // Get session ID from cookie
                if (!Request.Cookies.TryGetValue(Options.SessionCookieName, out var sessionId) ||
                    string.IsNullOrEmpty(sessionId))
                {
                    return AuthenticateResult.NoResult();
                }

                // Get session from cache
                var sessionInfo = await sessionCacheService.GetSessionAsync(sessionId);
                if (sessionInfo == null)
                {
                    _logger.LogWarning("Session {SessionId} not found in cache", sessionId);
                    return AuthenticateResult.NoResult();
                }

                // Check if session is expired
                if (sessionInfo.ExpiresAt < TimeProvider.GetUtcNow())
                {
                    _logger.LogWarning("Session {SessionId} has expired", sessionId);
                    await sessionCacheService.DeleteSessionAsync(sessionId);
                    return AuthenticateResult.NoResult();
                }

                // Create claims principal from session info
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, sessionInfo.UserId),
                    new(ClaimTypes.Name, sessionInfo.UserName),
                    new(ClaimTypes.Email, sessionInfo.Email),
                    new(ClaimTypes.GivenName, sessionInfo.FirstName),
                    new(ClaimTypes.Surname, sessionInfo.LastName),
                };

                // Add custom claims
                foreach (var claim in sessionInfo.Claims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }

                // Add role claims
                foreach (var role in sessionInfo.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Add session ID claim
                claims.Add(new Claim("session_id", sessionId));

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);

                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                _logger.LogDebug("Session {SessionId} authenticated successfully for user {UserId}",
                    sessionId, sessionInfo.UserId);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return AuthenticateResult.Fail(ex);
            }
        }
    }

    public class SessionAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public string SessionCookieName { get; set; } = "session_id";
        public string LoginPath { get; set; } = "/login";
        public string LogoutPath { get; set; } = "/logout";
        public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.FromDays(30);
        public bool SlidingExpiration { get; set; } = true;
    }
}