using FluentAssertions;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for Job entity.
/// Tests entity properties, defaults, and business logic methods.
/// </summary>
public class JobTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var job = new Job();

        // Assert
        job.Type.Should().Be(JobType.VideoProcessing);
        job.Status.Should().Be(JobStatus.Pending);
        job.Progress.Should().Be(0);
        job.CurrentStage.Should().Be(PipelineStage.None);
        job.RetryCount.Should().Be(0);
        job.MaxRetries.Should().Be(3);
        job.Priority.Should().Be(1);
        job.UserId.Should().Be(string.Empty);
    }

    [Fact]
    public void GetStageProgress_WhenStageProgressJsonIsNull_ReturnsEmptyDictionary()
    {
        // Arrange
        var job = new Job { StageProgressJson = null };

        // Act
        var result = job.GetStageProgress();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetStageProgress_WhenStageProgressJsonIsEmpty_ReturnsEmptyDictionary()
    {
        // Arrange
        var job = new Job { StageProgressJson = "" };

        // Act
        var result = job.GetStageProgress();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetStageProgress_WhenStageProgressJsonIsWhitespace_ReturnsEmptyDictionary()
    {
        // Arrange
        var job = new Job { StageProgressJson = "   " };

        // Act
        var result = job.GetStageProgress();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetStageProgress_WhenStageProgressJsonIsValid_ReturnsProgressDictionary()
    {
        // Arrange
        var job = new Job
        {
            StageProgressJson = "{\"Download\":50.0,\"AudioExtraction\":75.5}"
        };

        // Act
        var result = job.GetStageProgress();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[PipelineStage.Download].Should().Be(50.0);
        result[PipelineStage.AudioExtraction].Should().Be(75.5);
    }

    [Fact]
    public void GetStageProgress_WhenStageProgressJsonIsInvalid_ReturnsEmptyDictionary()
    {
        // Arrange
        var job = new Job { StageProgressJson = "invalid json" };

        // Act
        var result = job.GetStageProgress();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void SetStageProgress_ShouldUpdateStageProgressJson()
    {
        // Arrange
        var job = new Job();

        // Act
        job.SetStageProgress(PipelineStage.Download, 50);

        // Assert
        var result = job.GetStageProgress();
        result[PipelineStage.Download].Should().Be(50);
    }

    [Fact]
    public void SetStageProgress_WithMultipleStages_ShouldUpdateAllStages()
    {
        // Arrange
        var job = new Job();

        // Act
        job.SetStageProgress(PipelineStage.Download, 100);
        job.SetStageProgress(PipelineStage.AudioExtraction, 50);
        job.SetStageProgress(PipelineStage.Transcription, 25);

        // Assert
        var result = job.GetStageProgress();
        result[PipelineStage.Download].Should().Be(100);
        result[PipelineStage.AudioExtraction].Should().Be(50);
        result[PipelineStage.Transcription].Should().Be(25);
    }

    [Fact]
    public void SetStageProgress_ShouldClampProgressToZero_WhenNegativeValueProvided()
    {
        // Arrange
        var job = new Job();

        // Act
        job.SetStageProgress(PipelineStage.Download, -10);

        // Assert
        var result = job.GetStageProgress();
        result[PipelineStage.Download].Should().Be(0);
    }

    [Fact]
    public void SetStageProgress_ShouldClampProgressTo100_WhenExceedingValue()
    {
        // Arrange
        var job = new Job();

        // Act
        job.SetStageProgress(PipelineStage.Download, 150);

        // Assert
        var result = job.GetStageProgress();
        result[PipelineStage.Download].Should().Be(100);
    }

    [Fact]
    public void SetStageProgress_ShouldUpdateExistingStage()
    {
        // Arrange
        var job = new Job();
        job.SetStageProgress(PipelineStage.Download, 50);

        // Act
        job.SetStageProgress(PipelineStage.Download, 75);

        // Assert
        var result = job.GetStageProgress();
        result[PipelineStage.Download].Should().Be(75);
    }

    [Fact]
    public void CalculateOverallProgress_WhenNoStages_ReturnsZero()
    {
        // Arrange
        var job = new Job();

        // Act
        var progress = job.CalculateOverallProgress();

        // Assert
        progress.Should().Be(0);
    }

    [Fact]
    public void CalculateOverallProgress_WithSingleStage_CalculatesCorrectly()
    {
        // Arrange
        var job = new Job();
        job.SetStageProgress(PipelineStage.Download, 100); // Weight: 20

        // Act
        var progress = job.CalculateOverallProgress();

        // Assert
        progress.Should().Be(20); // 100% of 20% weight
    }

    [Fact]
    public void CalculateOverallProgress_WithAllStages_CalculatesCorrectly()
    {
        // Arrange
        var job = new Job();
        job.SetStageProgress(PipelineStage.Download, 100);         // 20% weight
        job.SetStageProgress(PipelineStage.AudioExtraction, 100);  // 15% weight
        job.SetStageProgress(PipelineStage.Transcription, 100);    // 50% weight
        job.SetStageProgress(PipelineStage.Segmentation, 100);     // 15% weight

        // Act
        var progress = job.CalculateOverallProgress();

        // Assert
        progress.Should().Be(100); // All stages complete
    }

    [Fact]
    public void CalculateOverallProgress_WithPartialStages_CalculatesCorrectly()
    {
        // Arrange
        var job = new Job();
        job.SetStageProgress(PipelineStage.Download, 100);         // 20% * 1.0 = 20
        job.SetStageProgress(PipelineStage.AudioExtraction, 100);  // 15% * 1.0 = 15
        job.SetStageProgress(PipelineStage.Transcription, 50);     // 50% * 0.5 = 25

        // Act
        var progress = job.CalculateOverallProgress();

        // Assert
        progress.Should().Be(60); // 20 + 15 + 25 = 60
    }

    [Fact]
    public void CalculateOverallProgress_WithHalfProgress_CalculatesCorrectly()
    {
        // Arrange
        var job = new Job();
        job.SetStageProgress(PipelineStage.Download, 50);
        job.SetStageProgress(PipelineStage.AudioExtraction, 50);
        job.SetStageProgress(PipelineStage.Transcription, 50);
        job.SetStageProgress(PipelineStage.Segmentation, 50);

        // Act
        var progress = job.CalculateOverallProgress();

        // Assert
        progress.Should().Be(50); // Half of all stages = 50%
    }

    [Fact]
    public void ErrorTracking_AllFieldsCanBeSet()
    {
        // Arrange
        var job = new Job();
        var errorMessage = "Test error";
        var stackTrace = "Stack trace here";
        var errorType = "HttpRequestException";
        var failedStage = PipelineStage.Download;

        // Act
        job.ErrorMessage = errorMessage;
        job.ErrorStackTrace = stackTrace;
        job.ErrorType = errorType;
        job.FailedStage = failedStage;

        // Assert
        job.ErrorMessage.Should().Be(errorMessage);
        job.ErrorStackTrace.Should().Be(stackTrace);
        job.ErrorType.Should().Be(errorType);
        job.FailedStage.Should().Be(failedStage);
    }

    [Fact]
    public void Retry_FieldsCanBeSet()
    {
        // Arrange
        var job = new Job();
        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        job.RetryCount = 2;
        job.MaxRetries = 5;
        job.NextRetryAt = nextRetry;

        // Assert
        job.RetryCount.Should().Be(2);
        job.MaxRetries.Should().Be(5);
        job.NextRetryAt.Should().Be(nextRetry);
    }

    [Fact]
    public void JobExecution_TimestampsCanBeSet()
    {
        // Arrange
        var job = new Job();
        var started = DateTime.UtcNow;
        var completed = DateTime.UtcNow.AddMinutes(10);

        // Act
        job.StartedAt = started;
        job.CompletedAt = completed;

        // Assert
        job.StartedAt.Should().Be(started);
        job.CompletedAt.Should().Be(completed);
    }

    [Fact]
    public void InheritsFromBaseEntity()
    {
        // Act
        var job = new Job();

        // Assert
        job.Should().BeAssignableTo<BaseEntity>();
        job.Id.Should().NotBeNullOrEmpty();
        job.CreatedAt.Should().NotBe(default(DateTime));
        job.UpdatedAt.Should().NotBe(default(DateTime));
    }
}
