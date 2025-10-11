using FluentAssertions;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for Video entity.
/// Tests entity properties, defaults, and relationships.
/// </summary>
public class VideoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var video = new Video();

        // Assert
        video.Title.Should().Be(string.Empty);
        video.Description.Should().BeNull();
        video.YouTubeId.Should().BeNull();
        video.Url.Should().BeNull();
        video.Status.Should().Be(VideoStatus.Pending);
        video.ProcessingStatus.Should().Be(VideoStatus.Pending);
        video.TranscriptionStatus.Should().Be(TranscriptionStatus.NotStarted);
        video.EmbeddingStatus.Should().Be(EmbeddingStatus.None);
        video.ProcessingProgress.Should().Be(0);
        video.EmbeddingProgress.Should().Be(0);
        video.Tags.Should().NotBeNull().And.BeEmpty();
        video.Jobs.Should().NotBeNull().And.BeEmpty();
        video.TranscriptSegments.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Title_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var title = "Test Video Title";

        // Act
        video.Title = title;

        // Assert
        video.Title.Should().Be(title);
    }

    [Fact]
    public void YouTubeId_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var youtubeId = "dQw4w9WgXcQ";

        // Act
        video.YouTubeId = youtubeId;

        // Assert
        video.YouTubeId.Should().Be(youtubeId);
    }

    [Fact]
    public void Status_CanBeUpdated()
    {
        // Arrange
        var video = new Video();

        // Act
        video.Status = VideoStatus.Processing;

        // Assert
        video.Status.Should().Be(VideoStatus.Processing);
    }

    [Fact]
    public void TranscriptionStatus_CanBeUpdated()
    {
        // Arrange
        var video = new Video();

        // Act
        video.TranscriptionStatus = TranscriptionStatus.InProgress;

        // Assert
        video.TranscriptionStatus.Should().Be(TranscriptionStatus.InProgress);
    }

    [Fact]
    public void EmbeddingStatus_CanBeUpdated()
    {
        // Arrange
        var video = new Video();

        // Act
        video.EmbeddingStatus = EmbeddingStatus.InProgress;

        // Assert
        video.EmbeddingStatus.Should().Be(EmbeddingStatus.InProgress);
    }

    [Fact]
    public void ProcessingProgress_CanBeUpdated()
    {
        // Arrange
        var video = new Video();

        // Act
        video.ProcessingProgress = 75;

        // Assert
        video.ProcessingProgress.Should().Be(75);
    }

    [Fact]
    public void Duration_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var duration = TimeSpan.FromMinutes(10);

        // Act
        video.Duration = duration;

        // Assert
        video.Duration.Should().Be(duration);
    }

    [Fact]
    public void Metadata_CanStoreJsonData()
    {
        // Arrange
        var video = new Video();
        var metadata = "{\"key\": \"value\"}";

        // Act
        video.Metadata = metadata;

        // Assert
        video.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void ErrorMessage_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var errorMessage = "Processing failed due to invalid format";

        // Act
        video.ErrorMessage = errorMessage;

        // Assert
        video.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void UserId_RequiredProperty()
    {
        // Arrange
        var video = new Video();
        var userId = "user-123";

        // Act
        video.UserId = userId;

        // Assert
        video.UserId.Should().Be(userId);
    }

    [Fact]
    public void Tags_CanBeAddedAndRetrieved()
    {
        // Arrange
        var video = new Video();
        var tags = new List<string> { "tag1", "tag2", "tag3" };

        // Act
        video.Tags = tags;

        // Assert
        video.Tags.Should().HaveCount(3);
        video.Tags.Should().Contain("tag1");
        video.Tags.Should().Contain("tag2");
        video.Tags.Should().Contain("tag3");
    }

    [Fact]
    public void PublishedAt_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var publishedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        video.PublishedAt = publishedDate;

        // Assert
        video.PublishedAt.Should().Be(publishedDate);
    }

    [Fact]
    public void ViewCount_CanBeSet()
    {
        // Arrange
        var video = new Video();

        // Act
        video.ViewCount = 1000;

        // Assert
        video.ViewCount.Should().Be(1000);
    }

    [Fact]
    public void LikeCount_CanBeSet()
    {
        // Arrange
        var video = new Video();

        // Act
        video.LikeCount = 50;

        // Assert
        video.LikeCount.Should().Be(50);
    }

    [Fact]
    public void ChannelInfo_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var channelId = "UC_channel_id";
        var channelTitle = "Test Channel";

        // Act
        video.ChannelId = channelId;
        video.ChannelTitle = channelTitle;

        // Assert
        video.ChannelId.Should().Be(channelId);
        video.ChannelTitle.Should().Be(channelTitle);
    }

    [Fact]
    public void TranscribedAt_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var transcribedAt = DateTime.UtcNow;

        // Act
        video.TranscribedAt = transcribedAt;

        // Assert
        video.TranscribedAt.Should().Be(transcribedAt);
    }

    [Fact]
    public void EmbeddedAt_CanBeSet()
    {
        // Arrange
        var video = new Video();
        var embeddedAt = DateTime.UtcNow;

        // Act
        video.EmbeddedAt = embeddedAt;

        // Assert
        video.EmbeddedAt.Should().Be(embeddedAt);
    }

    [Fact]
    public void InheritsFromBaseEntity()
    {
        // Act
        var video = new Video();

        // Assert
        video.Should().BeAssignableTo<BaseEntity>();
        video.Id.Should().NotBeNullOrEmpty();
        video.CreatedAt.Should().NotBe(default(DateTime));
        video.UpdatedAt.Should().NotBe(default(DateTime));
    }
}
