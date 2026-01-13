using EzioHost.Core.Mappers;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Core.Mappers;

public class MapperClassTests
{
    [Fact]
    public void MapperClass_ShouldBeInstantiable()
    {
        // Arrange & Act
        var mapperClass = new MapperClass();

        // Assert
        mapperClass.Should().NotBeNull();
    }

    [Fact]
    public void MapperClass_ShouldInheritFromProfile()
    {
        // Arrange & Act
        var mapperClass = new MapperClass();

        // Assert
        mapperClass.Should().BeAssignableTo<AutoMapper.Profile>();
    }
}
