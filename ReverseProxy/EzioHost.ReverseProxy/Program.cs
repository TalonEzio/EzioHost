using System.Net.Http.Headers;
using EzioHost.ReverseProxy.Extensions;
using EzioHost.Shared.Common;
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
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cfg =>
                {
                    cfg.LoginPath = "/login";//Map login from AuthController
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = settings.OpenIdConnect.Authority;
                    options.ClientId = settings.OpenIdConnect.ClientId;
                    options.ClientSecret = settings.OpenIdConnect.ClientSecret;

                    options.ResponseType = OpenIdConnectResponseType.Code;

                    options.SaveTokens = true;
                    //options.GetClaimsFromUserInfoEndpoint = true;

                    options.Scope.Add(OpenIdConnectScope.Email);
                    options.Scope.Add(OpenIdConnectScope.OfflineAccess);//need for refresh token if provider not set default
                    options.Scope.Add(settings.OpenIdConnect.WebApiScope);//need add this scope from oidc server

                    options.RequireHttpsMetadata = false;

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var claims = context.Principal.Claims.ToList();
                            var userId = claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                            if (userId != null)
                            {
                                //using var scope = context.HttpContext.RequestServices.CreateScope();
                                //var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                                //await userService.UpdateUserInfo(userId, email);
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
                        var user = transformContext.HttpContext.User;
                        if (user.Identity is { IsAuthenticated: true })
                        {
                            var token = await transformContext.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
                            Console.WriteLine($"Token: {token}");
                            transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
                cfg.CopyResponseHeaders = true;
            });
            app.UseForwardedHeaders();

            app.Run();
        }
    }
}
