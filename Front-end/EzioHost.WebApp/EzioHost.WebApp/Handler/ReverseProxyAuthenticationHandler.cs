using EzioHost.Shared.Common;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace EzioHost.WebApp.Handler
{
    public class ReverseProxyAuthenticationSchemeConstants
    {
        public const string AuthenticationScheme = "ReverseProxy.Oidc";
        public const string AuthenticationType = "ReverseProxy.Authentication";
    }
    public class ReverseProxyAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public string AccessDeniedPath { get; set; } = "/access-denied";//blazor page
        public string ReverseProxyBaseUrl { get; set; } = BaseUrlCommon.ReverseProxyUrl;
    }

    public class ReverseProxyAuthenticationHandler(
        IOptionsMonitor<ReverseProxyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        HttpClient httpClient,
        UrlEncoder encoder)
        : AuthenticationHandler<ReverseProxyAuthenticationSchemeOptions>(options, logger, encoder)
    {
        private readonly ILoggerFactory _logger = logger;
        private ILogger InnerLogger => _logger.CreateLogger(typeof(ReverseProxyAuthenticationHandler));

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var isBlazorComponent = Context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>() != null;
            var isBlazorJs = Request.Path.Equals("/_blazor", StringComparison.OrdinalIgnoreCase);
            var isLogout = !Request.Cookies.Any(x => x.Key.StartsWith(".AspNetCore.Cookies"));
            if (isLogout)
            {
                return AuthenticateResult.Fail($"Not authenticated - {DateTime.Now}");
            }
            if (!(isBlazorComponent || isBlazorJs))
                return AuthenticateResult.NoResult();
            try
            {
                var userInfo = await httpClient.GetFromJsonAsync<List<ClaimDto>>("user", new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userInfo != null)
                {
                    var claims = userInfo.Select(ClaimDto.ConvertToClaim).ToList();
                    var identity = new ClaimsIdentity(claims, ReverseProxyAuthenticationSchemeConstants.AuthenticationType);
                    var principal = new ClaimsPrincipal(identity);
                    InnerLogger.LogError($"{Request.Path} - User authenticated - {DateTime.Now}");
                    return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
                }
            }
            catch (Exception e)
            {
                InnerLogger.LogError(e, $"Authentication failed - {Request.Path} - {DateTime.Now}");
                return AuthenticateResult.Fail($"Not authenticated - {DateTime.Now}");
            }

            InnerLogger.LogWarning($"Cannot get user - {Request.Path} - {DateTime.Now}");
            return AuthenticateResult.Fail($"Not authenticated - {DateTime.Now}");
        }


        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var innerLogger = _logger.CreateLogger(typeof(ReverseProxyAuthenticationHandler));
            if (Context.User.Identity is { IsAuthenticated: true })
            {
                innerLogger.LogWarning($"Challenge ok - {DateTime.Now}");
                return Task.CompletedTask;

            }
            var challengeUrl = $"{Path.Combine(Options.ReverseProxyBaseUrl, "login")}?returnUrl={properties.RedirectUri}";
            innerLogger.LogWarning($"Challenge to {challengeUrl}");
            Response.Redirect(challengeUrl);
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            var innerLogger = _logger.CreateLogger(typeof(ReverseProxyAuthenticationHandler));

            innerLogger.LogWarning($"Forbidden to {Options.AccessDeniedPath}");
            Response.Redirect($"{Options.AccessDeniedPath}");
            return Task.CompletedTask;
        }
    }
}
