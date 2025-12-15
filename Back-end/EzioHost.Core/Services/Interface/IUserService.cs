using System.Linq.Expressions;
using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Interface;

public interface IUserService
{
    Task<User?> GetUserByCondition(Expression<Func<User, bool>> expression,
        Expression<Func<User, object>>[]? includes = null);

    Task<User> CreateNew(User newUser);
    Task<User> UpdateUser(User updateUser);
}