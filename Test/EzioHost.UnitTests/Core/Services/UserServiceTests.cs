using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Domain.Entities;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = TestMockFactory.CreateUserRepositoryMock();
        _userService = new UserService(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task GetUserByCondition_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = TestDataBuilder.CreateUser(id: userId);
        Expression<Func<User, bool>> expression = u => u.Id == userId;

        _userRepositoryMock
            .Setup(x => x.GetUserByCondition(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserByCondition(expression);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedUser);
        _userRepositoryMock.Verify(x => x.GetUserByCondition(expression, null), Times.Once);
    }

    [Fact]
    public async Task GetUserByCondition_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        Expression<Func<User, bool>> expression = u => u.Id == Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetUserByCondition(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByCondition(expression);

        // Assert
        result.Should().BeNull();
        _userRepositoryMock.Verify(x => x.GetUserByCondition(expression, null), Times.Once);
    }

    [Fact]
    public async Task GetUserByCondition_ShouldPassIncludes_WhenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = TestDataBuilder.CreateUser(id: userId);
        Expression<Func<User, bool>> expression = u => u.Id == userId;
        Expression<Func<User, object>>[] includes = [u => u.Id];

        _userRepositoryMock
            .Setup(x => x.GetUserByCondition(It.IsAny<Expression<Func<User, bool>>>(), includes))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserByCondition(expression, includes);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.GetUserByCondition(expression, includes), Times.Once);
    }

    [Fact]
    public async Task CreateNew_ShouldReturnCreatedUser()
    {
        // Arrange
        var newUser = TestDataBuilder.CreateUser();

        _userRepositoryMock
            .Setup(x => x.CreateNew(It.IsAny<User>()))
            .ReturnsAsync(newUser);

        // Act
        var result = await _userService.CreateNew(newUser);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(newUser);
        _userRepositoryMock.Verify(x => x.CreateNew(newUser), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdatedUser()
    {
        // Arrange
        var userToUpdate = TestDataBuilder.CreateUser();
        userToUpdate.FirstName = "Updated";

        _userRepositoryMock
            .Setup(x => x.UpdateUser(It.IsAny<User>()))
            .ReturnsAsync(userToUpdate);

        // Act
        var result = await _userService.UpdateUser(userToUpdate);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Updated");
        _userRepositoryMock.Verify(x => x.UpdateUser(userToUpdate), Times.Once);
    }
}
