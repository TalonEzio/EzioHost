using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace EzioHost.WebAPI.Startup
{
    public static class StaticFilesStartup
    {
        public static WebApplication ConfigureStaticFiles(this WebApplication app)
        {
            var wwwrootDirectory = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            if (!Directory.Exists(wwwrootDirectory))
            {
                Directory.CreateDirectory(wwwrootDirectory);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = new FileExtensionContentTypeProvider
                {
                    Mappings = { [".m3u8"] = "application/x-mpegURL" }
                },
                FileProvider = new PhysicalFileProvider(wwwrootDirectory),
            });

            return app;
        }
    }
}

