using Microsoft.EntityFrameworkCore;
using EzioHost.Infrastructure.SqlServer.DataContexts;

namespace EzioHost.IntegrationTests.WebAPI;

public class TestDatabaseFixture : IDisposable
{
    public EzioHostDbContext DbContext { get; private set; }

    public TestDatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        DbContext = new EzioHostDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }
}
