using EzioHost.ReverseProxy.Handlers;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EzioHost.ReverseProxy.Services
{
    public interface ISessionAuthenticationService
    {
        /// <summary>
        /// Tạo session mới sau khi OIDC authentication thành công
        /// </summary>
        /// <param name="httpContext">HttpContext hiện tại</param>
        /// <param name="principal">ClaimsPrincipal từ OIDC</param>
        /// <param name="properties">AuthenticationProperties từ OIDC</param>
        /// <returns>Session ID</returns>
        Task<string> CreateSessionAsync(HttpContext httpContext, ClaimsPrincipal principal, AuthenticationProperties properties);

        /// <summary>
        /// Refresh tokens trong session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="properties">AuthenticationProperties mới với tokens đã được refresh</param>
        Task RefreshSessionTokensAsync(string sessionId, AuthenticationProperties properties);

        /// <summary>
        /// Xóa session
        /// </summary>
        /// <param name="httpContext">HttpContext hiện tại</param>
        Task DeleteSessionAsync(HttpContext httpContext);
    }

    public class SessionAuthenticationService(
        ISessionCacheService sessionCacheService,
        ILogger<SessionAuthenticationService> logger,
        IOptionsMonitor<SessionAuthenticationSchemeOptions> options)
        : ISessionAuthenticationService
    {
        private readonly SessionAuthenticationSchemeOptions _options = options.CurrentValue;

        public async Task<string> CreateSessionAsync(HttpContext httpContext, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            try
            {
                var sessionId = sessionCacheService.GenerateSessionId();
                
                // Extract user information from claims
                if (principal.Identity is not ClaimsIdentity identity)
                {
                    throw new ArgumentException("Invalid claims principal");
                }

                var sessionInfo = new SessionInfo
                {
                    UserId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                    UserName = identity.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                    Email = identity.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                    FirstName = identity.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty,
                    LastName = identity.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastAccessedAt = DateTimeOffset.UtcNow,
                };

                // Extract tokens from authentication properties
                var tokens = properties.GetTokens().ToList();
                var accessToken = tokens.FirstOrDefault(t => t.Name == "access_token")?.Value ?? string.Empty;
                var idToken = tokens.FirstOrDefault(t => t.Name == "id_token")?.Value ?? string.Empty;
                var refreshToken = tokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value ?? string.Empty;
                var tokenType = tokens.FirstOrDefault(t => t.Name == "token_type")?.Value ?? "Bearer";
                
                // Parse expiration time
                var expiresAtText = tokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;
                sessionInfo.ExpiresAt = DateTimeOffset.TryParse(expiresAtText, out var expiresAt) 
                    ? expiresAt 
                    // Default to 30 days if no expiration found
                    : DateTimeOffset.UtcNow.AddDays(_options.ExpireTimeSpan.TotalDays);

                sessionInfo.AccessToken = accessToken;
                sessionInfo.IdToken = idToken;
                sessionInfo.RefreshToken = refreshToken;
                sessionInfo.TokenType = tokenType;

                // Extract additional claims
                foreach (var claim in identity.Claims)
                {
                    if (claim.Type != ClaimTypes.NameIdentifier && 
                        claim.Type != ClaimTypes.Name && 
                        claim.Type != ClaimTypes.Email && 
                        claim.Type != ClaimTypes.GivenName && 
                        claim.Type != ClaimTypes.Surname &&
                        claim.Type != ClaimTypes.Role)
                    {
                        sessionInfo.Claims[claim.Type] = claim.Value;
                    }
                }

                // Extract roles
                var roleClaims = identity.Claims.Where(c => c.Type == ClaimTypes.Role);
                sessionInfo.Roles = roleClaims.Select(c => c.Value).ToList();

                // Save session to cache
                await sessionCacheService.SetSessionAsync(sessionId, sessionInfo, _options.ExpireTimeSpan);

                // Set session cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    MaxAge = _options.ExpireTimeSpan,
                    Expires = DateTimeOffset.UtcNow.Add(_options.ExpireTimeSpan)
                };

                httpContext.Response.Cookies.Append(_options.SessionCookieName, sessionId, cookieOptions);

                logger.LogInformation("Created new session {SessionId} for user {UserId}", sessionId, sessionInfo.UserId);

                return sessionId;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating session");
                throw;
            }
        }

        public async Task RefreshSessionTokensAsync(string sessionId, AuthenticationProperties properties)
        {
            try
            {
                var sessionInfo = await sessionCacheService.GetSessionAsync(sessionId);
                if (sessionInfo == null)
                {
                    logger.LogWarning("Session {SessionId} not found for token refresh", sessionId);
                    return;
                }

                // Update tokens from authentication properties
                var tokens = properties.GetTokens().ToList();
                var accessToken = tokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
                var idToken = tokens.FirstOrDefault(t => t.Name == "id_token")?.Value;
                var refreshToken = tokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
                var tokenType = tokens.FirstOrDefault(t => t.Name == "token_type")?.Value;

                if (!string.IsNullOrEmpty(accessToken))
                    sessionInfo.AccessToken = accessToken;
                if (!string.IsNullOrEmpty(idToken))
                    sessionInfo.IdToken = idToken;
                if (!string.IsNullOrEmpty(refreshToken))
                    sessionInfo.RefreshToken = refreshToken;
                if (!string.IsNullOrEmpty(tokenType))
                    sessionInfo.TokenType = tokenType;

                // Update expiration time
                var expiresAtText = tokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;
                if (DateTimeOffset.TryParse(expiresAtText, out var expiresAt))
                {
                    sessionInfo.ExpiresAt = expiresAt;
                }

                // Update last accessed time
                sessionInfo.LastAccessedAt = DateTimeOffset.UtcNow;

                // Save updated session
                await sessionCacheService.SetSessionAsync(sessionId, sessionInfo, _options.ExpireTimeSpan);

                logger.LogDebug("Refreshed tokens for session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing tokens for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task DeleteSessionAsync(HttpContext httpContext)
        {
            try
            {
                if (httpContext.Request.Cookies.TryGetValue(_options.SessionCookieName, out var sessionId) && 
                    !string.IsNullOrEmpty(sessionId))
                {
                    await sessionCacheService.DeleteSessionAsync(sessionId);
                    
                    // Delete session cookie
                    httpContext.Response.Cookies.Delete(_options.SessionCookieName);

                    logger.LogInformation("Deleted session {SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting session");
                throw;
            }
        }
    }
}