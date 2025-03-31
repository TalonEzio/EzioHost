using EzioHost.Core.UnitOfWorks;
using EzioHost.Infrastructure.SqlServer.DataContext;
using Microsoft.EntityFrameworkCore.Storage;

namespace EzioHost.Infrastructure.SqlServer.UnitOfWorks
{
    public class BaseUnitOfWork(EzioHostDbContext dbContext) : IBaseUnitOfWork
    {
        private IDbContextTransaction? _transaction;
        public async Task BeginTransactionAsync()
        {
            _transaction = await dbContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction is null) throw new Exception("Error transaction, please open transaction first!");
            await dbContext.SaveChangesAsync();
            await _transaction.CommitAsync();

            _transaction.Dispose();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction is null) throw new Exception("Error transaction, please open transaction first!");
            await _transaction.RollbackAsync();

            _transaction.Dispose();
            _transaction = null;
        }
    }
}
