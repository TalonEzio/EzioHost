using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EzioHost.WebAPI.Startup;

public static class AuthenticationStartup
{
    public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder builder,
        AppSettings appSettings)
    {
        builder.Services.AddAuthentication(cfg =>
            {
                cfg.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                cfg.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(bearerOptions =>
                {
                    var jwtOidc = appSettings.JwtOidc;

                    bearerOptions.RequireHttpsMetadata = false;
                    bearerOptions.Audience = jwtOidc.Audience;
                    bearerOptions.MetadataAddress = jwtOidc.MetaDataAddress;
                    bearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOidc.Issuer,
                        ValidAudience = jwtOidc.Audience,
                        ClockSkew = TimeSpan.Zero
                    };

                    bearerOptions.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/hubs"))
                                context.Token = accessToken;

                            return Task.CompletedTask;
                        }
                    };
                }
            );

        return builder;
    }
}