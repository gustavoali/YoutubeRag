using FluentAssertions;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for BaseEntity class.
/// Tests entity initialization and property defaults.
/// </summary>
public class BaseEntityTests
{
    private class TestEntity : BaseEntity { }

    [Fact]
    public void Constructor_ShouldInitializeIdWithGuid()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(entity.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldInitializeCreatedAtWithCurrentDateTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var entity = new TestEntity();

        // Assert
        var afterCreation = DateTime.UtcNow.AddSeconds(1);
        entity.CreatedAt.Should().BeAfter(beforeCreation);
        entity.CreatedAt.Should().BeBefore(afterCreation);
        entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_ShouldInitializeUpdatedAtWithCurrentDateTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var entity = new TestEntity();

        // Assert
        var afterCreation = DateTime.UtcNow.AddSeconds(1);
        entity.UpdatedAt.Should().BeAfter(beforeCreation);
        entity.UpdatedAt.Should().BeBefore(afterCreation);
        entity.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIdsForDifferentInstances()
    {
        // Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void Id_CanBeSetManually()
    {
        // Arrange
        var entity = new TestEntity();
        var customId = "custom-test-id";

        // Act
        entity.Id = customId;

        // Assert
        entity.Id.Should().Be(customId);
    }

    [Fact]
    public void CreatedAt_CanBeSetManually()
    {
        // Arrange
        var entity = new TestEntity();
        var customDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        entity.CreatedAt = customDate;

        // Assert
        entity.CreatedAt.Should().Be(customDate);
    }

    [Fact]
    public void UpdatedAt_CanBeSetManually()
    {
        // Arrange
        var entity = new TestEntity();
        var customDate = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        // Act
        entity.UpdatedAt = customDate;

        // Assert
        entity.UpdatedAt.Should().Be(customDate);
    }
}
