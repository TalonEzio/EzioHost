using EzioHost.Domain.Entities;
using System.Linq.Expressions;

namespace EzioHost.Core.Services.Interface
{
    public interface IUserService
    {
        Task<User?> GetUserByCondition(Expression<Func<User, bool>> expression, Expression<Func<User, object>>[]? includes = null);
        Task<User> CreateNew(User newUser);
        Task<User> UpdateUser(User updateUser);
    }
}
