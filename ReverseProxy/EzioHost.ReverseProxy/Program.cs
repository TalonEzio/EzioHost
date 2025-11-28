using EzioHost.ReverseProxy.Extensions;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http.Headers;
using System.Security.Claims;
using EzioHost.Aspire.ServiceDefaults;
using EzioHost.Shared.Private.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                    cfg.LoginPath = "/login"; //Map login from AuthController
                    cfg.LogoutPath = "/logout"; //Map logout from AuthController

                    // Cookie persistence settings - quan trọng để cookie tồn tại sau khi đóng browser
                    cfg.ExpireTimeSpan = TimeSpan.FromDays(30); // Cookie có hiệu lực 30 ngày
                    cfg.SlidingExpiration = true; // Tự động gia hạn cookie khi user active
                    cfg.Cookie.MaxAge = TimeSpan.FromDays(30); // Max age của cookie
                    cfg.Cookie.HttpOnly = true; // Bảo mật
                    cfg.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS only khi có thể
                    cfg.Cookie.SameSite = SameSiteMode.Lax; // CSRF protection
                    cfg.Cookie.IsEssential = true; // Luôn lưu cookie kể cả khi user không consent
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
                    options.Scope.Add(OpenIdConnectScope.OfflineAccess); //need for refresh token if provider not set default
                    options.Scope.Add(settings.OpenIdConnect.WebApiScope); //custom scope from OIDC server

                    options.RequireHttpsMetadata = false;

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var user = context.Principal;

                            if (user is { Identity.IsAuthenticated: true })
                            {
                                using var scope = context.HttpContext.RequestServices.CreateScope();
                                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

                                var token = context.TokenEndpointResponse?.AccessToken;
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                                var identity = (ClaimsIdentity)user.Identity!;
                                var id = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                                var email = identity.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
                                var firstName = identity.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
                                var lastName = identity.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
                                var userName = identity.FindFirst(settings.OpenIdConnect.UserNameClaimType)?.Value ?? string.Empty;

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
                                    {
                                        identity.AddClaim(new Claim(ClaimTypes.Sid, userDto.Id.ToString()));
                                    }
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
            builder.Services.AddHttpClient(nameof(EzioHost),
                cfg => { cfg.BaseAddress = new Uri(BaseUrl.WebApiUrl); });

            builder.Services.AddScoped(serviceProvider =>
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(EzioHost)));

            // Cấu hình refresh token trước khi token expired 10 phút
            // Điều này đảm bảo token luôn được refresh tự động khi user mở lại browser
            builder.Services.ConfigureCookieOidcRefresh(
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
                TimeSpan.FromMinutes(10));

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
                            if (user.Identity is ClaimsIdentity && user.Identity.IsAuthenticated)
                            {
                                var accessToken = await transformContext.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
                                transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                                //var subClaimValue = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                                //if (!string.IsNullOrEmpty(subClaimValue))
                                //{
                                //    if (!transformContext.ProxyRequest.Headers.Contains("X-USER-ID"))
                                //    {
                                //        transformContext.ProxyRequest.Headers.Add("X-USER-ID", subClaimValue);
                                //    }
                                //}
                            }
                        }
                    });
                });

            builder.Services.AddAntiforgery();

            builder.Services.AddCors(cfg =>
            {
                cfg.AddPolicy(nameof(EzioHost), policyBuilder =>
                {
                    policyBuilder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });


            //Log.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(builder.Configuration)
            //    .CreateLogger();

            //builder.Host.UseSerilog();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseCors(nameof(EzioHost));

            //app.UseSerilogRequestLogging(opts =>
            //{
            //    opts.IncludeQueryInRequestPath = true;
            //});

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            app.MapControllers();
            
            app.MapReverseProxy();

            app.Run();
        }

    }
}
