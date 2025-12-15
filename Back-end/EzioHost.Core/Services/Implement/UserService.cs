using System.Linq.Expressions;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Implement;

public class UserService(IUserRepository userRepository) : IUserService
{
    public Task<User?> GetUserByCondition(Expression<Func<User, bool>> expression,
        Expression<Func<User, object>>[]? includes = null)
    {
        return userRepository.GetUserByCondition(expression, includes);
    }

    public Task<User> CreateNew(User newUser)
    {
        return userRepository.CreateNew(newUser);
    }

    public Task<User> UpdateUser(User updateUser)
    {
        return userRepository.UpdateUser(updateUser);
    }
}