using System.Linq.Expressions;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class UserSqlServerRepositoryTests : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly UserSqlServerRepository _repository;

    public UserSqlServerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new UserSqlServerRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetUserByCondition_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        Expression<Func<User, bool>> expression = u => u.Email == user.Email;

        // Act
        var result = await _repository.GetUserByCondition(expression);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetUserByCondition_ShouldReturnNull_WhenDoesNotExist()
    {
        // Arrange
        Expression<Func<User, bool>> expression = u => u.Email == "nonexistent@example.com";

        // Act
        var result = await _repository.GetUserByCondition(expression);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateNew_ShouldAddUserToDatabase()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();

        // Act
        var result = await _repository.CreateNew(user);

        // Assert
        result.Should().NotBeNull();
        var userInDb = await _dbContext.Users.FindAsync(result.Id);
        userInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUser_ShouldUpdateInDatabase()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        user.FirstName = "Updated";

        // Act
        var result = await _repository.UpdateUser(user);

        // Assert
        result.FirstName.Should().Be("Updated");
    }
}