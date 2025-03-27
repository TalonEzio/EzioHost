using System.Diagnostics;
using System.Linq.Expressions;
using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EzioHost.Infrastructure.SqlServer.Repositories
{
    public class UserSqlServerRepository(EzioHostDbContext dbContext,ILogger<UserSqlServerRepository> logger) : IUserRepository
    {
        public Task<User?> GetUserByCondition(Expression<Func<User, bool>> expression, Expression<Func<User, object>>[]? includes = null)
        {
            var users = dbContext.Users.AsQueryable();

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    users = users.Include(include);
                }
            }


            return users.FirstOrDefaultAsync(expression);
        }

        public async Task<User> CreateNew(User newUser)
        {
            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();
            return newUser;
        }

        public async Task<User> UpdateUser(User updateUser)
        {
            dbContext.Users.Update(updateUser);
            await dbContext.SaveChangesAsync();
            return updateUser;
        }
    }
}
