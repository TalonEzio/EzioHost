using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using EzioHost.Shared.Common;
using EzioHost.WebApp.Components;
using EzioHost.WebApp.Handler;
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
                    cfg.ReverseProxyBaseUrl = BaseUrlConstants.ReverseProxyUrl;
                });
        builder.Services.AddAuthorization();

        // Add HttpClient
        builder.Services.AddTransient<RequestCookieHandler>();
        builder.Services.AddHttpClient(nameof(EzioHost), cfg =>
        {
            cfg.BaseAddress = new Uri(BaseUrlConstants.ReverseProxyUrl);
        })
            .AddHttpMessageHandler<RequestCookieHandler>()
            ;

        builder.Services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(EzioHost)));
        builder.Services.AddHttpContextAccessor();

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
        });

        builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration.GetSection(nameof(AppSettings)));

        //builder.Services.AddSignalR(x =>
        //{
        //    x.MaximumReceiveMessageSize = 1 * 1024 * 1024; // 1MB per message
        //});

        builder.Services.AddBlazorise(cfg =>
            {
                cfg.Immediate = true;
            })
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();

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

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
    }
}
