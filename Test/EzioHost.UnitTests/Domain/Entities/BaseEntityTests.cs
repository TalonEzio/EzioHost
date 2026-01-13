using EzioHost.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Entities;

public class BaseEntityTests
{
    [Fact]
    public void BaseCreatedEntity_ShouldSetCreatedAt()
    {
        // Arrange
        var entity = new BaseCreatedEntity();

        // Act
        entity.CreatedAt = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BaseAuditableEntity_ShouldSetModifiedAt()
    {
        // Arrange
        var entity = new BaseAuditableEntity();

        // Act
        entity.ModifiedAt = DateTime.UtcNow;

        // Assert
        entity.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BaseAuditableEntity_IsDeleted_ShouldReturnFalse_WhenDeletedAtIsNull()
    {
        // Arrange
        var entity = new BaseAuditableEntity();

        // Act & Assert
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void BaseAuditableEntity_IsDeleted_ShouldReturnTrue_WhenDeletedAtIsSet()
    {
        // Arrange
        var entity = new BaseAuditableEntity();

        // Act
        entity.DeletedAt = DateTime.UtcNow;

        // Assert
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void BaseCreatedEntityWithUserId_ShouldSetCreatedBy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entity = new BaseCreatedEntityWithUserId<Guid>();

        // Act
        entity.CreatedBy = userId;

        // Assert
        entity.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public void BaseAuditableEntityWithUserId_ShouldSetCreatedByAndModifiedBy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entity = new BaseAuditableEntityWithUserId<Guid>();

        // Act
        entity.CreatedBy = userId;
        entity.ModifiedBy = userId;

        // Assert
        entity.CreatedBy.Should().Be(userId);
        entity.ModifiedBy.Should().Be(userId);
    }
}