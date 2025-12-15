using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace EzioHost.ReverseProxy.Handlers;

// https://github.com/dotnet/aspnetcore/issues/8175
internal sealed class CookieOidcRefresher(
    IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
    ILogger<CookieOidcRefresher> logger)
{
    private readonly OpenIdConnectProtocolValidator _oidcTokenValidator = new()
    {
        // We no longer have the original nonce cookie which is deleted at the end of the authorization code flow having served its purpose.
        // Even if we had the nonce, it's likely expired. It's not intended for refresh requests. Otherwise, we'd use oidcOptions.ProtocolValidator.
        RequireNonce = false
    };

    public async Task ValidateOrRefreshCookieAsync(CookieValidatePrincipalContext validateContext, string oidcScheme,
        TimeSpan refreshTimeSpan)
    {
        var accessTokenExpirationText = validateContext.Properties.GetTokenValue("expires_at");
        var refreshToken = validateContext.Properties.GetTokenValue("refresh_token");

        // Kiểm tra xem có refresh token không
        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("No refresh token found in cookie, rejecting principal");
            validateContext.RejectPrincipal();
            return;
        }

        // Nếu không parse được expiration time, coi như đã expired và cần refresh
        if (!DateTimeOffset.TryParse(accessTokenExpirationText, out var accessTokenExpiration))
        {
            logger.LogWarning("Cannot parse token expiration time, attempting refresh");
            await RefreshTokensAsync(validateContext, oidcScheme, refreshToken);
            return;
        }

        var oidcOptions = oidcOptionsMonitor.Get(oidcScheme);
        var now = oidcOptions.TimeProvider!.GetUtcNow();

        // Kiểm tra nếu token đã expired hoặc sắp expired (trong khoảng refreshTimeSpan)
        if (now >= accessTokenExpiration || now + refreshTimeSpan >= accessTokenExpiration)
        {
            logger.LogInformation(
                "Token expired or expiring soon, refreshing token. Expires at: {ExpiresAt}, Current time: {Now}",
                accessTokenExpiration, now);
            await RefreshTokensAsync(validateContext, oidcScheme, refreshToken);
            return;
        }

        // Token còn hạn, không cần refresh
        logger.LogDebug("Token still valid, expires at: {ExpiresAt}", accessTokenExpiration);
    }

    private async Task RefreshTokensAsync(CookieValidatePrincipalContext validateContext, string oidcScheme,
        string refreshToken)
    {
        try
        {
            var oidcOptions = oidcOptionsMonitor.Get(oidcScheme);
            var oidcConfiguration =
                await oidcOptions.ConfigurationManager!.GetConfigurationAsync(
                    validateContext.HttpContext.RequestAborted);
            var tokenEndpoint = oidcConfiguration.TokenEndpoint;

            if (string.IsNullOrEmpty(tokenEndpoint))
            {
                logger.LogError("Cannot refresh cookie. TokenEndpoint missing!");
                validateContext.RejectPrincipal();
                return;
            }

            var refreshRequestContent = new Dictionary<string, string?>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = oidcOptions.ClientId,
                ["client_secret"] = oidcOptions.ClientSecret,
                ["scope"] = string.Join(" ", oidcOptions.Scope),
                ["refresh_token"] = refreshToken
            };

            logger.LogDebug("Attempting to refresh token with endpoint: {TokenEndpoint}", tokenEndpoint);

            using var refreshResponse = await oidcOptions.Backchannel.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(refreshRequestContent),
                validateContext.HttpContext.RequestAborted);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                var errorContent = await refreshResponse.Content.ReadAsStringAsync();
                logger.LogError("Token refresh failed with status {StatusCode}. Response: {Response}",
                    refreshResponse.StatusCode, errorContent);
                validateContext.RejectPrincipal();
                return;
            }

            var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
            var message = new OpenIdConnectMessage(refreshJson);

            // Validate the new ID token
            var validationParameters = oidcOptions.TokenValidationParameters.Clone();
            if (oidcOptions.ConfigurationManager is BaseConfigurationManager baseConfigurationManager)
            {
                validationParameters.ConfigurationManager = baseConfigurationManager;
            }
            else
            {
                validationParameters.ValidIssuer = oidcConfiguration.Issuer;
                validationParameters.IssuerSigningKeys = oidcConfiguration.SigningKeys;
            }

            var validationResult =
                await oidcOptions.TokenHandler.ValidateTokenAsync(message.IdToken, validationParameters);

            if (!validationResult.IsValid)
            {
                logger.LogError("Token validation failed after refresh");
                validateContext.RejectPrincipal();
                return;
            }

            var validatedIdToken = JwtSecurityTokenConverter.Convert(validationResult.SecurityToken as JsonWebToken);
            validatedIdToken.Payload["nonce"] = null;

            _oidcTokenValidator.ValidateTokenResponse(new OpenIdConnectProtocolValidationContext
            {
                ProtocolMessage = message,
                ClientId = oidcOptions.ClientId,
                ValidatedIdToken = validatedIdToken
            });

            // Update the principal and tokens
            validateContext.ShouldRenew = true;
            validateContext.ReplacePrincipal(new ClaimsPrincipal(validationResult.ClaimsIdentity));

            var now = oidcOptions.TimeProvider!.GetUtcNow();
            var expiresIn = int.Parse(message.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var expiresAt = now + TimeSpan.FromSeconds(expiresIn);

            // Preserve the refresh token if not provided in response
            var newRefreshToken = !string.IsNullOrEmpty(message.RefreshToken)
                ? message.RefreshToken
                : refreshToken;

            validateContext.Properties.StoreTokens([
                new AuthenticationToken { Name = "access_token", Value = message.AccessToken },
                new AuthenticationToken { Name = "id_token", Value = message.IdToken },
                new AuthenticationToken { Name = "refresh_token", Value = newRefreshToken },
                new AuthenticationToken { Name = "token_type", Value = message.TokenType },
                new AuthenticationToken
                    { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) }
            ]);

            // Đảm bảo cookie được persist
            validateContext.Properties.IsPersistent = true;
            validateContext.Properties.AllowRefresh = true;

            logger.LogInformation("Token refreshed successfully. New expiration: {ExpiresAt}", expiresAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while refreshing token");
            validateContext.RejectPrincipal();
        }
    }
}