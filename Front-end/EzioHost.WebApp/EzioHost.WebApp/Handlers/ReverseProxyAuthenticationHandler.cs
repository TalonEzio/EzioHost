using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.Extensions.Options;

namespace EzioHost.WebApp.Handlers;

public class ReverseProxyAuthenticationSchemeConstants
{
    public const string AuthenticationScheme = "ReverseProxy.Oidc";
    public const string AuthenticationType = "ReverseProxy.Authentication";
}

public class ReverseProxyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string AccessDeniedPath { get; set; } = "/access-denied"; //blazor page
    public string ReverseProxyBaseUrl { get; set; } = string.Empty;
}

public class ReverseProxyAuthenticationHandler(
    IOptionsMonitor<ReverseProxyAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    IHttpClientFactory httpClientFactory,
    UrlEncoder encoder)
    : AuthenticationHandler<ReverseProxyAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var isBlazorComponent = Context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>() != null;
        var isBlazorJs = Request.Path.Equals("/_blazor", StringComparison.OrdinalIgnoreCase);

        var hasAuthCookie = Request.Cookies.Any(x => x.Key.StartsWith(".AspNetCore.Cookies"));
        if (!hasAuthCookie)
        {
            Logger.LogDebug("No authentication cookie found at {Path}", Request.Path);
            return AuthenticateResult.Fail("Not authenticated - missing cookie");
        }

        if (!(isBlazorComponent || isBlazorJs)) return AuthenticateResult.NoResult();

        try
        {
            var httpClient = httpClientFactory.CreateClient(nameof(EzioHost));

            var userInfoClaims = await httpClient.GetFromJsonAsync<List<ClaimDto>>("user", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfoClaims is { Count: > 0 })
            {
                var claims = userInfoClaims.Select(ClaimDto.ConvertToClaim).ToList();
                var identity = new ClaimsIdentity(claims, ReverseProxyAuthenticationSchemeConstants.AuthenticationType);
                var principal = new ClaimsPrincipal(identity);

                Logger.LogInformation("User authenticated successfully at {Path}", Request.Path);
                return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
            }

            Logger.LogWarning("User info is empty at {Path}", Request.Path);
            return AuthenticateResult.Fail("User info is empty");
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request failed during authentication at {Path}", Request.Path);
            return AuthenticateResult.Fail($"Authentication service unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Authentication failed at {Path}", Request.Path);
            return AuthenticateResult.Fail($"Authentication error: {ex.Message}");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Context.User.Identity is { IsAuthenticated: true })
        {
            Logger.LogDebug("User already authenticated, no challenge needed");
            return Task.CompletedTask;
        }

        var returnUrl = properties.RedirectUri ?? Request.Path.ToString();
        var challengeUrl =
            $"{Options.ReverseProxyBaseUrl.TrimEnd('/')}/login?returnUrl={Uri.EscapeDataString(returnUrl)}";

        Logger.LogInformation("Redirecting to authentication challenge: {ChallengeUrl}", challengeUrl);
        Response.Redirect(challengeUrl);
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Logger.LogWarning("Access forbidden, redirecting to {AccessDeniedPath}", Options.AccessDeniedPath);
        Response.Redirect(Options.AccessDeniedPath);
        return Task.CompletedTask;
    }
}