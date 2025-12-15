using EzioHost.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EzioHost.WebAPI.Startup;

public static class SignalRStartup
{
    public static WebApplicationBuilder ConfigureSignalR(this WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR(cfg => { });

        builder.Services.AddSingleton<IUserIdProvider, ReverseProxyUserIdProvider>();

        return builder;
    }
}