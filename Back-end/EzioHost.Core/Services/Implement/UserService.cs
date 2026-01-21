using System.Linq.Expressions;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EzioHost.Core.Services.Implement;

public class UserService(
    IUserRepository userRepository,
    ILogger<UserService> logger) : IUserService
{
    public Task<User?> GetUserByCondition(Expression<Func<User, bool>> expression,
        Expression<Func<User, object>>[]? includes = null)
    {
        logger.LogDebug("Getting user by condition. HasIncludes: {HasIncludes}", includes != null && includes.Length > 0);
        return userRepository.GetUserByCondition(expression, includes);
    }

    public async Task<User> CreateNew(User newUser)
    {
        logger.LogInformation("Creating new user. UserId: {UserId}", newUser.Id);
        var result = await userRepository.CreateNew(newUser);
        logger.LogInformation("Successfully created user {UserId}", newUser.Id);
        return result;
    }

    public async Task<User> UpdateUser(User updateUser)
    {
        logger.LogInformation("Updating user. UserId: {UserId}", updateUser.Id);
        var result = await userRepository.UpdateUser(updateUser);
        logger.LogInformation("Successfully updated user {UserId}", updateUser.Id);
        return result;
    }
}