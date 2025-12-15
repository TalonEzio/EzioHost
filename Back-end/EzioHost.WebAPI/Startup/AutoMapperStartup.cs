using EzioHost.Core.Mappers;

namespace EzioHost.WebAPI.Startup;

public static class AutoMapperStartup
{
    public static WebApplicationBuilder ConfigureAutoMapper(this WebApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(cfg => { cfg.AddMaps(typeof(MapperClass)); });

        return builder;
    }
}