using System.Net.Http.Headers;
using System.Security.Claims;
using EzioHost.ReverseProxy.Extensions;
using Yarp.ReverseProxy.Transforms;

namespace EzioHost.ReverseProxy.Startup;

public static class ReverseProxyStartup
{
    public static WebApplicationBuilder ConfigureReverseProxy(this WebApplicationBuilder builder)
    {
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
                            var accessToken = await transformContext.HttpContext.GetDownstreamAccessTokenAsync();
                            transformContext.ProxyRequest.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", accessToken);
                        }
                    }
                });
            });

        return builder;
    }
}