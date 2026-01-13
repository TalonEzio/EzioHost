using EzioHost.Infrastructure.SqlServer.DataContexts;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.IntegrationTests.WebAPI;

public class TestDatabaseFixture : IDisposable
{
    public TestDatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        DbContext = new EzioHostDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public EzioHostDbContext DbContext { get; }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }
}