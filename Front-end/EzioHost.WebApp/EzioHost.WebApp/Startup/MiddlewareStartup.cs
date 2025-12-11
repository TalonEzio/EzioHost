using EzioHost.Aspire.ServiceDefaults;
using EzioHost.WebApp.Components;

namespace EzioHost.WebApp.Startup
{
    public static class MiddlewareStartup
    {
        public static WebApplication ConfigureMiddleware(this WebApplication app)
        {
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

            return app;
        }
    }
}

