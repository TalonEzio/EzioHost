using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Security.Claims;
using EzioHost.ReverseProxy.Extensions;
using EzioHost.Shared.Common;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

namespace EzioHost.ReverseProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration.GetSection(nameof(AppSettings)));

            var settings = new AppSettings();
            builder.Configuration.Bind(nameof(AppSettings), settings);

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cfg =>
                {
                    cfg.LoginPath = "/login";//Map login from AuthController
                    cfg.LogoutPath = "/logout";//Map logout from AuthController
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = settings.OpenIdConnect.Authority;
                    options.ClientId = settings.OpenIdConnect.ClientId;
                    options.ClientSecret = settings.OpenIdConnect.ClientSecret;

                    options.ResponseType = OpenIdConnectResponseType.Code;

                    options.SaveTokens = false;
                    //options.GetClaimsFromUserInfoEndpoint = true;

                    options.Scope.Add(OpenIdConnectScope.Email);
                    options.Scope.Add(OpenIdConnectScope.OfflineAccess);//need for refresh token if provider not set default
                    options.Scope.Add(settings.OpenIdConnect.WebApiScope);//need add this scope from OIDC server

                    options.RequireHttpsMetadata = false;

                    options.Events = new OpenIdConnectEvents
                    {
                        //Sync OIDC <-> Database
                        OnTokenValidated = async context =>
                        {
                            var user = context.Principal;

                            if (user is { Identity.IsAuthenticated: true })
                            {
                                using var scope = context.HttpContext.RequestServices.CreateScope();
                                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

                                var token = context.TokenEndpointResponse?.AccessToken;
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                                var claims = user.Claims.ToImmutableList();
                                var body = new UserCreateUpdateRequestDto()
                                {
                                    Email = claims.First(x => x.Type == ClaimTypes.Email).Value,
                                    FirstName = claims.First(x => x.Type == ClaimTypes.GivenName).Value,
                                    LastName = claims.First(x => x.Type == ClaimTypes.Surname).Value,
                                    UserName = claims.First(x => x.Type == settings.OpenIdConnect.UserNameClaimType).Value,
                                };
                                try
                                {
                                    var response = await httpClient.PostAsJsonAsync("api/User", body);
                                    response.EnsureSuccessStatusCode();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        },

                        OnUserInformationReceived = context =>
                        {
                            var claims = context.User.ToString();
                            Console.WriteLine($"UserInfo received: {claims}");

                            return Task.CompletedTask;
                        },
                    };
                });
            builder.Services.AddHttpClient(nameof(EzioHost), cfg =>
            {
                cfg.BaseAddress = new Uri(BaseUrlConstants.ReverseProxyUrl);
            });

            builder.Services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(EzioHost)));

            builder.Services.ConfigureCookieOidcRefresh(
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
                TimeSpan.FromMinutes(5));

            builder.Services.AddAuthorization();


            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                //Transform for webapi
                .AddTransforms(transformsBuilderContext =>
                {
                    transformsBuilderContext.AddRequestTransform(async transformContext =>
                    {
                        if (transformContext.HttpContext.Request.Path.StartsWithSegments("/api"))
                        {
                            var user = transformContext.HttpContext.User;
                            if (user.Identity is { IsAuthenticated: true })
                            {
                                var token = await transformContext.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
                                transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            }
                        }
                    });
                });

            builder.Services.AddAntiforgery();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            app.MapReverseProxy();

            app.MapControllers();

            //Forward to frontend
            app.MapForwarder("{**rest}", BaseUrlConstants.FrontendUrl, cfg =>
            {
                cfg.CopyRequestHeaders = true;
            });

            app.Run();
        }
    }
}
