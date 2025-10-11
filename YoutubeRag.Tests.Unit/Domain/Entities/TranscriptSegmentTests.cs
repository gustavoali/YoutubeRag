using FluentAssertions;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for TranscriptSegment entity.
/// Tests entity properties, defaults, and computed properties.
/// </summary>
public class TranscriptSegmentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var segment = new TranscriptSegment();

        // Assert
        segment.VideoId.Should().Be(string.Empty);
        segment.Text.Should().Be(string.Empty);
        segment.StartTime.Should().Be(0);
        segment.EndTime.Should().Be(0);
        segment.SegmentIndex.Should().Be(0);
        segment.EmbeddingVector.Should().BeNull();
        segment.Confidence.Should().BeNull();
        segment.Language.Should().BeNull();
        segment.Speaker.Should().BeNull();
    }

    [Fact]
    public void VideoId_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();
        var videoId = "video-123";

        // Act
        segment.VideoId = videoId;

        // Assert
        segment.VideoId.Should().Be(videoId);
    }

    [Fact]
    public void Text_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();
        var text = "This is a transcript segment";

        // Act
        segment.Text = text;

        // Assert
        segment.Text.Should().Be(text);
    }

    [Fact]
    public void StartTime_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();

        // Act
        segment.StartTime = 10.5;

        // Assert
        segment.StartTime.Should().Be(10.5);
    }

    [Fact]
    public void EndTime_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();

        // Act
        segment.EndTime = 15.75;

        // Assert
        segment.EndTime.Should().Be(15.75);
    }

    [Fact]
    public void SegmentIndex_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();

        // Act
        segment.SegmentIndex = 42;

        // Assert
        segment.SegmentIndex.Should().Be(42);
    }

    [Fact]
    public void EmbeddingVector_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();
        var embeddingVector = "[0.1, 0.2, 0.3, 0.4]";

        // Act
        segment.EmbeddingVector = embeddingVector;

        // Assert
        segment.EmbeddingVector.Should().Be(embeddingVector);
    }

    [Fact]
    public void Confidence_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();

        // Act
        segment.Confidence = 0.95;

        // Assert
        segment.Confidence.Should().Be(0.95);
    }

    [Fact]
    public void Language_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();

        // Act
        segment.Language = "en";

        // Assert
        segment.Language.Should().Be("en");
    }

    [Fact]
    public void Speaker_CanBeSet()
    {
        // Arrange
        var segment = new TranscriptSegment();

        // Act
        segment.Speaker = "Speaker 1";

        // Assert
        segment.Speaker.Should().Be("Speaker 1");
    }

    [Fact]
    public void HasEmbedding_ReturnsFalse_WhenEmbeddingVectorIsNull()
    {
        // Arrange
        var segment = new TranscriptSegment { EmbeddingVector = null };

        // Act
        var hasEmbedding = segment.HasEmbedding;

        // Assert
        hasEmbedding.Should().BeFalse();
    }

    [Fact]
    public void HasEmbedding_ReturnsFalse_WhenEmbeddingVectorIsEmpty()
    {
        // Arrange
        var segment = new TranscriptSegment { EmbeddingVector = "" };

        // Act
        var hasEmbedding = segment.HasEmbedding;

        // Assert
        hasEmbedding.Should().BeFalse();
    }

    [Fact]
    public void HasEmbedding_ReturnsFalse_WhenEmbeddingVectorIsWhitespace()
    {
        // Arrange
        var segment = new TranscriptSegment { EmbeddingVector = "   " };

        // Act
        var hasEmbedding = segment.HasEmbedding;

        // Assert
        hasEmbedding.Should().BeFalse();
    }

    [Fact]
    public void HasEmbedding_ReturnsTrue_WhenEmbeddingVectorHasValue()
    {
        // Arrange
        var segment = new TranscriptSegment { EmbeddingVector = "[0.1, 0.2, 0.3]" };

        // Act
        var hasEmbedding = segment.HasEmbedding;

        // Assert
        hasEmbedding.Should().BeTrue();
    }

    [Fact]
    public void SegmentDuration_CanBeCalculatedFromStartAndEndTime()
    {
        // Arrange
        var segment = new TranscriptSegment
        {
            StartTime = 10.0,
            EndTime = 15.5
        };

        // Act
        var duration = segment.EndTime - segment.StartTime;

        // Assert
        duration.Should().Be(5.5);
    }

    [Fact]
    public void CompleteSegment_AllPropertiesSet()
    {
        // Arrange & Act
        var segment = new TranscriptSegment
        {
            VideoId = "video-123",
            Text = "Hello, this is a test.",
            StartTime = 0.0,
            EndTime = 2.5,
            SegmentIndex = 0,
            EmbeddingVector = "[0.1, 0.2, 0.3]",
            Confidence = 0.98,
            Language = "en",
            Speaker = "Speaker 1"
        };

        // Assert
        segment.VideoId.Should().Be("video-123");
        segment.Text.Should().Be("Hello, this is a test.");
        segment.StartTime.Should().Be(0.0);
        segment.EndTime.Should().Be(2.5);
        segment.SegmentIndex.Should().Be(0);
        segment.EmbeddingVector.Should().Be("[0.1, 0.2, 0.3]");
        segment.Confidence.Should().Be(0.98);
        segment.Language.Should().Be("en");
        segment.Speaker.Should().Be("Speaker 1");
        segment.HasEmbedding.Should().BeTrue();
    }

    [Fact]
    public void InheritsFromBaseEntity()
    {
        // Act
        var segment = new TranscriptSegment();

        // Assert
        segment.Should().BeAssignableTo<BaseEntity>();
        segment.Id.Should().NotBeNullOrEmpty();
        segment.CreatedAt.Should().NotBe(default(DateTime));
        segment.UpdatedAt.Should().NotBe(default(DateTime));
    }
}
