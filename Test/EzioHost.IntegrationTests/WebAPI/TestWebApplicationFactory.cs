using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.WebAPI;
using System.Security.Claims;

namespace EzioHost.IntegrationTests.WebAPI;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Override authentication to use test authentication handler
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            });
            
            // Add test authentication that always succeeds
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
            
            // Remove ALL DbContext-related service registrations completely
            var descriptorsToRemove = new List<ServiceDescriptor>();
            
            // Collect all services first to avoid modifying collection while iterating
            var allServices = services.ToList();
            
            foreach (var service in allServices)
            {
                // Remove DbContextOptions<EzioHostDbContext>
                if (service.ServiceType == typeof(DbContextOptions<EzioHostDbContext>))
                {
                    descriptorsToRemove.Add(service);
                    continue;
                }
                
                // Remove generic DbContextOptions<> for EzioHostDbContext
                if (service.ServiceType.IsGenericType)
                {
                    var genericTypeDef = service.ServiceType.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(DbContextOptions<>))
                    {
                        var args = service.ServiceType.GetGenericArguments();
                        if (args.Length > 0 && args[0] == typeof(EzioHostDbContext))
                        {
                            descriptorsToRemove.Add(service);
                            continue;
                        }
                    }
                    
                    // Remove IDbContextFactory<>
                    if (genericTypeDef == typeof(IDbContextFactory<>))
                    {
                        var args = service.ServiceType.GetGenericArguments();
                        if (args.Length > 0 && args[0] == typeof(EzioHostDbContext))
                        {
                            descriptorsToRemove.Add(service);
                            continue;
                        }
                    }
                }
                
                // Remove EzioHostDbContext registration
                if (service.ServiceType == typeof(EzioHostDbContext))
                {
                    descriptorsToRemove.Add(service);
                    continue;
                }
                
                // Remove if implementation type is EzioHostDbContext
                if (service.ImplementationType == typeof(EzioHostDbContext))
                {
                    descriptorsToRemove.Add(service);
                }
            }
            
            // Remove all collected descriptors
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }
            
            // Add in-memory database with a completely fresh registration
            // Use a unique database name per test to avoid conflicts
            var dbName = "TestDb_" + Guid.NewGuid();
            services.AddDbContext<EzioHostDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                // CRITICAL: Disable service provider caching to prevent SqlServer provider from being cached
                options.EnableServiceProviderCaching(false);
            }, ServiceLifetime.Scoped);
        });
    }
}
