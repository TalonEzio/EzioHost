using EzioHost.Core.Mappers;

namespace EzioHost.WebAPI.Startup;

public static class AutoMapperStartup
{
    public static WebApplicationBuilder ConfigureAutoMapper(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<StaticPathResolver>();
        builder.Services.AddAutoMapper(cfg => { cfg.AddProfile(typeof(MapperClass)); });

        return builder;
    }
}