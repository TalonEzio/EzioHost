namespace EzioHost.Core.UnitOfWorks
{
    public interface IBaseUnitOfWork
    {
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

    }
}
