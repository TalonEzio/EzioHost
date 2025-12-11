namespace EzioHost.WebApp.Startup
{
    public static class RazorComponentsStartup
    {
        public static WebApplicationBuilder ConfigureRazorComponents(this WebApplicationBuilder builder)
        {
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

            return builder;
        }
    }
}

