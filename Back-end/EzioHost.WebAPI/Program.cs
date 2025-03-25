
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

namespace EzioHost.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var appSettings = new AppSettings();

            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.Configuration.Bind(nameof(AppSettings), appSettings);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddAuthentication(cfg =>
                {
                    cfg.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    cfg.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    var jwtOidc = appSettings.JwtOidc;

                    x.RequireHttpsMetadata = false;
                    x.Audience = jwtOidc.Audience;
                    x.MetadataAddress = jwtOidc.MetaDataAddress;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOidc.Issuer,
                        ValidAudience = jwtOidc.Audience
                    };
                });

            var app = builder.Build();


            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            //var wwwrootPath = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    FileProvider = new PhysicalFileProvider(wwwrootPath),
            //    RequestPath = "/static"
            //});
            app.MapStaticAssets();

            app.MapControllers();



            app.Run();
        }
    }
}
