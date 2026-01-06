using System.Net.Http.Headers;
using System.Security.Claims;
using EzioHost.ReverseProxy.Extensions;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EzioHost.ReverseProxy.Startup;

public static class AuthenticationStartup
{
    public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder builder,
        AppSettings settings)
    {
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration.GetSection(nameof(AppSettings)));


        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cfg =>
            {
                cfg.LoginPath = "/login"; //Map login from AuthController
                cfg.LogoutPath = "/logout"; //Map logout from AuthController

                cfg.ExpireTimeSpan = TimeSpan.FromDays(30);
                cfg.SlidingExpiration = true;
                cfg.Cookie.MaxAge = TimeSpan.FromDays(30);
                cfg.Cookie.HttpOnly = true;
                cfg.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                cfg.Cookie.SameSite = SameSiteMode.Lax;
                cfg.Cookie.IsEssential = true;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = settings.OpenIdConnect.Authority;
                options.ClientId = settings.OpenIdConnect.ClientId;
                options.ClientSecret = settings.OpenIdConnect.ClientSecret;

                options.ResponseType = OpenIdConnectResponseType.Code;

                options.SaveTokens = true;
                //options.GetClaimsFromUserInfoEndpoint = true;

                options.Scope.Add(OpenIdConnectScope
                    .OfflineAccess); //need for refresh token if provider not set default
                options.Scope.Add(settings.OpenIdConnect.WebApiScope); //custom scope

                options.RequireHttpsMetadata = false;

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var user = context.Principal;

                        if (user is { Identity.IsAuthenticated: true })
                        {
                            using var scope = context.HttpContext.RequestServices.CreateScope();
                            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                            var httpClient = httpClientFactory.CreateClient(nameof(EzioHost));

                            var token = context.TokenEndpointResponse?.AccessToken;
                            httpClient.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Bearer", token);

                            var identity = (ClaimsIdentity)user.Identity!;
                            var id = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                            var email = identity.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
                            var firstName = identity.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
                            var lastName = identity.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
                            var userName = identity.FindFirst(settings.OpenIdConnect.UserNameClaimType)?.Value ??
                                           string.Empty;

                            var userCreateUpdateRequestDto = new UserCreateUpdateRequestDto
                            {
                                Id = Guid.Parse(id),
                                Email = email,
                                FirstName = firstName,
                                LastName = lastName,
                                UserName = userName
                            };
                            try
                            {
                                var response = await httpClient.PostAsJsonAsync("api/User", userCreateUpdateRequestDto);
                                response.EnsureSuccessStatusCode();

                                var userDto = await response.Content.ReadFromJsonAsync<UserCreateUpdateResponseDto>();

                                if (userDto?.Id != null)
                                    identity.AddClaim(new Claim(ClaimTypes.Sid, userDto.Id.ToString()));
                            }
                            catch (Exception e)
                            {
                                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                                logger.LogError(e, "Error creating/updating user in database");
                            }

                            context.Principal = new ClaimsPrincipal(identity);
                        }
                    }
                };
            });

        builder.Services.ConfigureCookieOidcRefresh(
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme,
            TimeSpan.FromMinutes(1));

        builder.Services.AddAuthorization();

        return builder;
    }
}