namespace EzioHost.WebApp.Startup;

public static class SignalRStartup
{
    public static WebApplicationBuilder ConfigureSignalR(this WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR(x =>
        {
            x.MaximumReceiveMessageSize = 1 * 1024 * 1024; // 1MB per message
            x.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
            x.HandshakeTimeout = TimeSpan.FromMinutes(1);
        });

        return builder;
    }
}