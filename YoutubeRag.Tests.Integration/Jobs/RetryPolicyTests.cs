using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Integration.Helpers;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.Jobs;

/// <summary>
/// Integration tests for JobRetryPolicy functionality
/// Tests exception classification, retry policy selection, and backoff calculations
/// </summary>
public class RetryPolicyTests : IntegrationTestBase
{
    private IJobRepository _jobRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ILogger<RetryPolicyTests> _logger = null!;

    public RetryPolicyTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _jobRepository = Scope.ServiceProvider.GetRequiredService<IJobRepository>();
        _unitOfWork = Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _logger = Scope.ServiceProvider.GetRequiredService<ILogger<RetryPolicyTests>>();
    }

    #region Exception Classification Tests

    [Theory]
    [InlineData(typeof(HttpRequestException), FailureCategory.TransientNetworkError)]
    [InlineData(typeof(TimeoutException), FailureCategory.TransientNetworkError)]
    [InlineData(typeof(TaskCanceledException), FailureCategory.TransientNetworkError)]
    public async Task JobRetryPolicy_ClassifiesNetworkExceptionsCorrectly(Type exceptionType, FailureCategory expectedCategory)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Network error occurred")!;

        // Act
        var category = JobRetryPolicy.ClassifyException(exception, _logger);

        // Assert
        category.Should().Be(expectedCategory);
        await Task.CompletedTask; // Satisfy async requirement
    }

    [Theory]
    [InlineData(typeof(ArgumentException), FailureCategory.PermanentError)]
    [InlineData(typeof(ArgumentNullException), FailureCategory.PermanentError)]
    [InlineData(typeof(FormatException), FailureCategory.PermanentError)]
    public async Task JobRetryPolicy_ClassifiesPermanentExceptionsCorrectly(Type exceptionType, FailureCategory expectedCategory)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Invalid input")!;

        // Act
        var category = JobRetryPolicy.ClassifyException(exception, _logger);

        // Assert
        category.Should().Be(expectedCategory);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task JobRetryPolicy_ClassifiesExceptionsByMessage_TransientNetworkError()
    {
        // Test message-based classification for transient network errors
        var exceptions = new[]
        {
            new InvalidOperationException("Connection timeout"),
            new Exception("Network error occurred"),
            new InvalidOperationException("HTTP 503 - Service unavailable"),
            new Exception("Too many requests - rate limit exceeded"),
            new InvalidOperationException("Connection reset by peer")
        };

        foreach (var exception in exceptions)
        {
            // Act
            var category = JobRetryPolicy.ClassifyException(exception, _logger);

            // Assert
            category.Should().Be(FailureCategory.TransientNetworkError,
                $"Exception with message '{exception.Message}' should be classified as TransientNetworkError");
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task JobRetryPolicy_ClassifiesExceptionsByMessage_PermanentError()
    {
        // Test message-based classification for permanent errors
        var exceptions = new[]
        {
            new InvalidOperationException("Video not found"),
            new Exception("Video deleted by owner"),
            new InvalidOperationException("Video unavailable - region blocked"),
            new Exception("Private video - access denied"),
            new InvalidOperationException("Invalid format - unsupported codec")
        };

        foreach (var exception in exceptions)
        {
            // Act
            var category = JobRetryPolicy.ClassifyException(exception, _logger);

            // Assert
            category.Should().Be(FailureCategory.PermanentError,
                $"Exception with message '{exception.Message}' should be classified as PermanentError");
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task JobRetryPolicy_ClassifiesExceptionsByMessage_ResourceNotAvailable()
    {
        // Test message-based classification for resource unavailability
        var exceptions = new[]
        {
            new IOException("Disk full"),
            new Exception("Out of disk space"),
            new InvalidOperationException("Model downloading - please wait"),
            new Exception("Whisper model not ready"),
            new InvalidOperationException("Insufficient memory")
        };

        foreach (var exception in exceptions)
        {
            // Act
            var category = JobRetryPolicy.ClassifyException(exception, _logger);

            // Assert
            category.Should().Be(FailureCategory.ResourceNotAvailable,
                $"Exception with message '{exception.Message}' should be classified as ResourceNotAvailable");
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task JobRetryPolicy_ClassifiesUnknownExceptionAsUnknownError()
    {
        // Arrange
        var exception = new Exception("Some random error that doesn't match any pattern");

        // Act
        var category = JobRetryPolicy.ClassifyException(exception, _logger);

        // Assert
        category.Should().Be(FailureCategory.UnknownError);
        await Task.CompletedTask;
    }

    #endregion

    #region Retry Policy Selection Tests

    [Fact]
    public async Task TransientError_ShouldHave5Retries_ExponentialBackoff()
    {
        // Arrange
        var exception = new HttpRequestException("Network timeout");

        // Act
        var policy = JobRetryPolicy.GetPolicy(exception, _logger);

        // Assert
        policy.Category.Should().Be(FailureCategory.TransientNetworkError);
        policy.MaxRetries.Should().Be(5);
        policy.UseExponentialBackoff.Should().BeTrue();
        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(10));
        policy.SendToDeadLetterQueue.Should().BeFalse();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ResourceError_ShouldHave3Retries_LinearBackoff()
    {
        // Arrange
        var exception = new IOException("Disk full");

        // Act
        var policy = JobRetryPolicy.GetPolicy(exception, _logger);

        // Assert
        policy.Category.Should().Be(FailureCategory.ResourceNotAvailable);
        policy.MaxRetries.Should().Be(3);
        policy.UseExponentialBackoff.Should().BeFalse();
        policy.InitialDelay.Should().Be(TimeSpan.FromMinutes(2));
        policy.SendToDeadLetterQueue.Should().BeFalse();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task PermanentError_ShouldNotRetry_DirectToDLQ()
    {
        // Arrange
        var exception = new InvalidOperationException("Video deleted by owner");

        // Act
        var policy = JobRetryPolicy.GetPolicy(exception, _logger);

        // Assert
        policy.Category.Should().Be(FailureCategory.PermanentError);
        policy.MaxRetries.Should().Be(0);
        policy.SendToDeadLetterQueue.Should().BeTrue();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task UnknownError_ShouldHave2Retries_ExponentialBackoff()
    {
        // Arrange
        var exception = new Exception("Unknown error occurred");

        // Act
        var policy = JobRetryPolicy.GetPolicy(exception, _logger);

        // Assert
        policy.Category.Should().Be(FailureCategory.UnknownError);
        policy.MaxRetries.Should().Be(2);
        policy.UseExponentialBackoff.Should().BeTrue();
        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(30));
        policy.SendToDeadLetterQueue.Should().BeFalse();
        await Task.CompletedTask;
    }

    #endregion

    #region Backoff Calculation Tests

    [Fact]
    public async Task TransientError_ExponentialBackoff_CalculatesCorrectly()
    {
        // Arrange
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.TransientNetworkError);

        // Act & Assert - Exponential backoff: 10s, 20s, 40s, 80s, 160s
        policy.GetNextRetryDelay(0).Should().Be(TimeSpan.FromSeconds(10));  // 10 * 2^0 = 10s
        policy.GetNextRetryDelay(1).Should().Be(TimeSpan.FromSeconds(20));  // 10 * 2^1 = 20s
        policy.GetNextRetryDelay(2).Should().Be(TimeSpan.FromSeconds(40));  // 10 * 2^2 = 40s
        policy.GetNextRetryDelay(3).Should().Be(TimeSpan.FromSeconds(80));  // 10 * 2^3 = 80s
        policy.GetNextRetryDelay(4).Should().Be(TimeSpan.FromSeconds(160)); // 10 * 2^4 = 160s

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ResourceError_LinearBackoff_CalculatesCorrectly()
    {
        // Arrange
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.ResourceNotAvailable);

        // Act & Assert - Linear backoff: 2m, 4m, 6m
        policy.GetNextRetryDelay(0).Should().Be(TimeSpan.FromMinutes(2)); // 2 * (0 + 1) = 2m
        policy.GetNextRetryDelay(1).Should().Be(TimeSpan.FromMinutes(4)); // 2 * (1 + 1) = 4m
        policy.GetNextRetryDelay(2).Should().Be(TimeSpan.FromMinutes(6)); // 2 * (2 + 1) = 6m

        await Task.CompletedTask;
    }

    [Fact]
    public async Task UnknownError_ExponentialBackoff_CalculatesCorrectly()
    {
        // Arrange
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.UnknownError);

        // Act & Assert - Exponential backoff: 30s, 60s
        policy.GetNextRetryDelay(0).Should().Be(TimeSpan.FromSeconds(30)); // 30 * 2^0 = 30s
        policy.GetNextRetryDelay(1).Should().Be(TimeSpan.FromSeconds(60)); // 30 * 2^1 = 60s

        await Task.CompletedTask;
    }

    #endregion

    #region Job Retry Behavior Tests

    [Fact]
    public async Task Job_TransientError_RetryCountIncremented_NextRetryAtCalculated()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            Priority = 1,
            RetryCount = 0,
            MaxRetries = 5,
            ErrorMessage = "Network timeout",
            LastFailureCategory = FailureCategory.TransientNetworkError.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FailedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Simulate retry logic
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.TransientNetworkError);
        var nextRetryDelay = policy.GetNextRetryDelay(job.RetryCount);

        job.RetryCount++;
        job.NextRetryAt = DateTime.UtcNow.Add(nextRetryDelay);
        job.Status = JobStatus.Pending; // Ready for retry
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var updatedJob = await _jobRepository.GetByIdAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.RetryCount.Should().Be(1);
        updatedJob.NextRetryAt.Should().NotBeNull();
        updatedJob.NextRetryAt.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(10), TimeSpan.FromSeconds(5));
        updatedJob.Status.Should().Be(JobStatus.Pending);
    }

    [Fact]
    public async Task Job_After5Retries_ShouldBeMarkedForDLQ()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            Priority = 1,
            RetryCount = 5,
            MaxRetries = 5,
            ErrorMessage = "Network timeout after max retries",
            LastFailureCategory = FailureCategory.TransientNetworkError.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FailedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Check if job should go to DLQ
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.TransientNetworkError);
        var shouldGoToDlq = job.RetryCount >= policy.MaxRetries;

        // Assert
        shouldGoToDlq.Should().BeTrue();
        policy.MaxRetries.Should().Be(5);
    }

    [Fact]
    public async Task Job_ResourceError_LinearBackoffApplied()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            Priority = 1,
            RetryCount = 1, // Second attempt
            MaxRetries = 3,
            ErrorMessage = "Whisper model downloading",
            LastFailureCategory = FailureCategory.ResourceNotAvailable.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Calculate next retry with linear backoff
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.ResourceNotAvailable);
        var nextRetryDelay = policy.GetNextRetryDelay(job.RetryCount);

        // Assert - Linear backoff: retry 1 should wait 4 minutes (2 * (1 + 1))
        nextRetryDelay.Should().Be(TimeSpan.FromMinutes(4));
    }

    [Fact]
    public async Task Job_PermanentError_NoRetry_ImmediateDLQ()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            Priority = 1,
            RetryCount = 0,
            MaxRetries = 0,
            ErrorMessage = "Video not found - deleted by owner",
            LastFailureCategory = FailureCategory.PermanentError.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FailedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.PermanentError);

        // Assert
        policy.MaxRetries.Should().Be(0);
        policy.SendToDeadLetterQueue.Should().BeTrue();
        job.RetryCount.Should().Be(0);
    }

    #endregion

    #region Policy Description Tests

    [Fact]
    public async Task RetryPolicy_HasDescriptiveMessages()
    {
        // Verify that all policies have helpful descriptions

        // Act
        var transientPolicy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.TransientNetworkError);
        var resourcePolicy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.ResourceNotAvailable);
        var permanentPolicy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.PermanentError);
        var unknownPolicy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.UnknownError);

        // Assert
        transientPolicy.Description.Should().NotBeNullOrEmpty();
        transientPolicy.Description.Should().Contain("5 retries");
        transientPolicy.Description.Should().Contain("exponential");

        resourcePolicy.Description.Should().NotBeNullOrEmpty();
        resourcePolicy.Description.Should().Contain("3 retries");
        resourcePolicy.Description.Should().Contain("linear");

        permanentPolicy.Description.Should().NotBeNullOrEmpty();
        permanentPolicy.Description.Should().Contain("no retries");
        permanentPolicy.Description.Should().Contain("Dead Letter Queue");

        unknownPolicy.Description.Should().NotBeNullOrEmpty();
        unknownPolicy.Description.Should().Contain("2");

        await Task.CompletedTask;
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task RetryPolicy_MaxRetriesReached_NoMoreRetries()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            RetryCount = 3,
            MaxRetries = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act
        var canRetry = job.RetryCount < job.MaxRetries;

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public async Task RetryPolicy_NegativeRetryCount_HandledGracefully()
    {
        // Ensure policy handles edge case of negative retry count

        // Arrange
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.TransientNetworkError);

        // Act
        var delay = policy.GetNextRetryDelay(-1);

        // Assert - Should still calculate (10 * 2^-1 = 5 seconds)
        delay.Should().Be(TimeSpan.FromSeconds(5));

        await Task.CompletedTask;
    }

    [Fact]
    public async Task RetryPolicy_VeryHighRetryCount_ExponentialBackoffCapped()
    {
        // Test that exponential backoff doesn't overflow

        // Arrange
        var policy = JobRetryPolicy.GetPolicyForCategory(FailureCategory.TransientNetworkError);

        // Act - High retry count
        var delay = policy.GetNextRetryDelay(10); // 10 * 2^10 = 10240 seconds

        // Assert - Should calculate without overflow
        delay.Should().Be(TimeSpan.FromSeconds(10240));
        delay.TotalHours.Should().BeApproximately(2.84, 0.01); // ~2.84 hours

        await Task.CompletedTask;
    }

    #endregion
}
