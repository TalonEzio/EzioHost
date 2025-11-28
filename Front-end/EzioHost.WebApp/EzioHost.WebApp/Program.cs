using EzioHost.Aspire.ServiceDefaults;
using EzioHost.Shared.Private.Endpoints;
using EzioHost.WebApp.Components;
using EzioHost.WebApp.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace EzioHost.WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddHubOptions(cfg =>
            {
                cfg.MaximumReceiveMessageSize = 12 * 1024 * 1024;
                //cfg.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                //cfg.HandshakeTimeout = TimeSpan.FromSeconds(30);
                //cfg.KeepAliveInterval = TimeSpan.FromSeconds(15);
            })
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization(cfg =>
            {
                cfg.SerializeAllClaims = true;
            });

        // Add Authentication
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, ReverseProxyAuthenticationStateProvider>();

        builder.Services.AddAuthentication(cfg =>
            {
                cfg.DefaultScheme = ReverseProxyAuthenticationSchemeConstants.AuthenticationScheme;
                cfg.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                cfg.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;

            })
            //custom oidc cookie handler
            .AddScheme<ReverseProxyAuthenticationSchemeOptions, ReverseProxyAuthenticationHandler>(
                ReverseProxyAuthenticationSchemeConstants.AuthenticationScheme,
                cfg =>
                {
                    cfg.ReverseProxyBaseUrl = BaseUrl.ReverseProxyUrl;
                });
        builder.Services.AddAuthorization();

        // Add HttpClient Factory
        builder.Services.AddTransient<RequestCookieHandler>();
        builder.Services.AddHttpClient(nameof(EzioHost), cfg =>
        {
            cfg.BaseAddress = new Uri(BaseUrl.ReverseProxyUrl);
        })
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
        });

        builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration.GetSection(nameof(AppSettings)));

        builder.Services.AddSignalR(x =>
        {
            x.MaximumReceiveMessageSize = 1 * 1024 * 1024; // 1MB per message
            x.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
            x.HandshakeTimeout = TimeSpan.FromMinutes(1);
        });

        builder.Services.AddCors(cfg =>
        {
            cfg.AddPolicy(nameof(EzioHost), policyBuilder =>
            {
                policyBuilder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });


        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors(nameof(EzioHost));

        app.UseAntiforgery();

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode(cfg => cfg.ContentSecurityFrameAncestorsPolicy = "*")
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);




        app.Run();
    }
}
