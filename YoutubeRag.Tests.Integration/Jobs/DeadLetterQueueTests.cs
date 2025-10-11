using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Services;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Integration.Helpers;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.Jobs;

/// <summary>
/// Integration tests for Dead Letter Queue functionality
/// Tests job failure handling, DLQ entry creation, requeue operations, and statistics
/// </summary>
public class DeadLetterQueueTests : IntegrationTestBase
{
    private IDeadLetterJobRepository _deadLetterRepository = null!;
    private IJobRepository _jobRepository = null!;
    private IVideoRepository _videoRepository = null!;
    private IUnitOfWork _unitOfWork = null!;

    public DeadLetterQueueTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Get repositories from scope
        _deadLetterRepository = Scope.ServiceProvider.GetRequiredService<IDeadLetterJobRepository>();
        _jobRepository = Scope.ServiceProvider.GetRequiredService<IJobRepository>();
        _videoRepository = Scope.ServiceProvider.GetRequiredService<IVideoRepository>();
        _unitOfWork = Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    #region Job Exceeds Max Retries Tests

    [Fact]
    public async Task Job_ExceedsMaxRetries_ShouldMoveToDeadLetterQueue()
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
            RetryCount = 5,
            MaxRetries = 5,
            ErrorMessage = "Test error after max retries",
            LastFailureCategory = FailureCategory.TransientNetworkError.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FailedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Simulate moving to DLQ (would normally be done by job processor)
        var deadLetterJob = new DeadLetterJob
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            FailureReason = "MaxRetriesExceeded",
            FailureDetails = JsonSerializer.Serialize(new
            {
                ErrorMessage = job.ErrorMessage,
                LastFailureCategory = job.LastFailureCategory,
                StackTrace = "Simulated stack trace for test"
            }),
            OriginalPayload = JsonSerializer.Serialize(new
            {
                VideoId = job.VideoId,
                JobType = job.Type.ToString(),
                Parameters = job.Parameters
            }),
            FailedAt = DateTime.UtcNow,
            AttemptedRetries = job.RetryCount,
            IsRequeued = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _deadLetterRepository.AddAsync(deadLetterJob);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var dlqEntry = await _deadLetterRepository.GetByJobIdAsync(job.Id);
        dlqEntry.Should().NotBeNull();
        dlqEntry!.FailureReason.Should().Be("MaxRetriesExceeded");
        dlqEntry.AttemptedRetries.Should().Be(5);
        dlqEntry.IsRequeued.Should().BeFalse();
        dlqEntry.FailureDetails.Should().NotBeNullOrEmpty();
        dlqEntry.FailureDetails.Should().Contain("StackTrace");
        dlqEntry.FailureDetails.Should().Contain("Test error after max retries");
        dlqEntry.OriginalPayload.Should().NotBeNullOrEmpty();
        dlqEntry.OriginalPayload.Should().Contain(video.Id);
    }

    [Fact]
    public async Task Job_ExceedsMaxRetries_PreservesFailureDetails()
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
            ErrorMessage = "Network timeout after multiple retries",
            LastFailureCategory = FailureCategory.TransientNetworkError.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        var exceptionDetails = new
        {
            ExceptionType = "HttpRequestException",
            Message = job.ErrorMessage,
            StackTrace = "   at System.Net.Http.HttpClient.SendAsync()\n   at YoutubeRag.Services.VideoDownload()",
            InnerException = "SocketException: Connection timed out",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var deadLetterJob = new DeadLetterJob
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            FailureReason = "MaxRetriesExceeded",
            FailureDetails = JsonSerializer.Serialize(exceptionDetails),
            OriginalPayload = JsonSerializer.Serialize(new { VideoId = job.VideoId }),
            FailedAt = DateTime.UtcNow,
            AttemptedRetries = job.RetryCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _deadLetterRepository.AddAsync(deadLetterJob);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var dlqEntry = await _deadLetterRepository.GetByJobIdAsync(job.Id);
        dlqEntry.Should().NotBeNull();

        var deserializedDetails = JsonSerializer.Deserialize<JsonElement>(dlqEntry!.FailureDetails!);
        deserializedDetails.GetProperty("ExceptionType").GetString().Should().Be("HttpRequestException");
        deserializedDetails.GetProperty("StackTrace").GetString().Should().Contain("HttpClient.SendAsync");
        deserializedDetails.GetProperty("InnerException").GetString().Should().Contain("SocketException");
    }

    #endregion

    #region Permanent Error Tests

    [Fact]
    public async Task Job_PermanentError_ShouldGoDirectlyToDLQ()
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
            RetryCount = 0, // No retries for permanent errors
            MaxRetries = 0,
            ErrorMessage = "Video not found - it has been deleted by the owner",
            LastFailureCategory = FailureCategory.PermanentError.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FailedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(job);
        await DbContext.SaveChangesAsync();

        // Act - Permanent error goes directly to DLQ
        var deadLetterJob = new DeadLetterJob
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            FailureReason = "PermanentError",
            FailureDetails = JsonSerializer.Serialize(new
            {
                ErrorMessage = job.ErrorMessage,
                FailureCategory = job.LastFailureCategory,
                Reason = "Video deleted by owner - cannot be recovered"
            }),
            OriginalPayload = JsonSerializer.Serialize(new { VideoId = job.VideoId }),
            FailedAt = DateTime.UtcNow,
            AttemptedRetries = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _deadLetterRepository.AddAsync(deadLetterJob);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var dlqEntry = await _deadLetterRepository.GetByJobIdAsync(job.Id);
        dlqEntry.Should().NotBeNull();
        dlqEntry!.FailureReason.Should().Be("PermanentError");
        dlqEntry.AttemptedRetries.Should().Be(0); // No retries attempted
        dlqEntry.FailureDetails.Should().Contain("cannot be recovered");
    }

    [Fact]
    public async Task Job_PermanentError_ClassifiedCorrectly()
    {
        // Test that JobRetryPolicy correctly identifies permanent errors
        var permanentExceptions = new Exception[]
        {
            new InvalidOperationException("Video not found"),
            new ArgumentException("Invalid video format"),
            new InvalidOperationException("Video unavailable - region blocked")
        };

        foreach (var exception in permanentExceptions)
        {
            // Act
            var policy = JobRetryPolicy.GetPolicy(exception);

            // Assert
            policy.Category.Should().Be(FailureCategory.PermanentError);
            policy.MaxRetries.Should().Be(0);
            policy.SendToDeadLetterQueue.Should().BeTrue();
        }
    }

    #endregion

    #region Requeue Tests

    [Fact]
    public async Task DeadLetterJob_Requeue_ShouldCreateNewJob()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var originalJob = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            Priority = 1,
            RetryCount = 5,
            MaxRetries = 5,
            Parameters = JsonSerializer.Serialize(new { Model = "base", Language = "en" }),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(originalJob);

        var deadLetterJob = new DeadLetterJob
        {
            Id = Guid.NewGuid().ToString(),
            JobId = originalJob.Id,
            FailureReason = "MaxRetriesExceeded",
            FailureDetails = JsonSerializer.Serialize(new { Error = "Network timeout" }),
            OriginalPayload = JsonSerializer.Serialize(new
            {
                VideoId = originalJob.VideoId,
                JobType = originalJob.Type.ToString(),
                Parameters = originalJob.Parameters
            }),
            FailedAt = DateTime.UtcNow,
            AttemptedRetries = 5,
            IsRequeued = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _deadLetterRepository.AddAsync(deadLetterJob);
        await _unitOfWork.SaveChangesAsync();

        // Act - Simulate requeue operation
        var newJob = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = originalJob.VideoId,
            UserId = originalJob.UserId,
            Type = originalJob.Type,
            Status = JobStatus.Pending,
            Priority = 1,
            RetryCount = 0,
            MaxRetries = 3,
            Parameters = originalJob.Parameters,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _jobRepository.AddAsync(newJob);

        // Mark DLQ entry as requeued
        await _deadLetterRepository.MarkAsRequeuedAsync(deadLetterJob.Id, AuthenticatedUserId!);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var updatedDlqEntry = await _deadLetterRepository.GetByIdAsync(deadLetterJob.Id);
        updatedDlqEntry.Should().NotBeNull();
        updatedDlqEntry!.IsRequeued.Should().BeTrue();
        updatedDlqEntry.RequeuedAt.Should().NotBeNull();
        updatedDlqEntry.RequeuedBy.Should().Be(AuthenticatedUserId);

        var newJobFromDb = await _jobRepository.GetByIdAsync(newJob.Id);
        newJobFromDb.Should().NotBeNull();
        newJobFromDb!.Status.Should().Be(JobStatus.Pending);
        newJobFromDb.RetryCount.Should().Be(0); // Fresh start
        newJobFromDb.Parameters.Should().Be(originalJob.Parameters); // Same parameters
    }

    [Fact]
    public async Task DeadLetterJob_Requeue_PreservesOriginalParameters()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var originalParameters = new
        {
            WhisperModel = "medium",
            Language = "es",
            TranslateToEnglish = true,
            OutputFormat = "json"
        };

        var originalJob = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = video.Id,
            UserId = video.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            Parameters = JsonSerializer.Serialize(originalParameters),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Jobs.AddAsync(originalJob);

        var deadLetterJob = new DeadLetterJob
        {
            Id = Guid.NewGuid().ToString(),
            JobId = originalJob.Id,
            FailureReason = "MaxRetriesExceeded",
            OriginalPayload = JsonSerializer.Serialize(new
            {
                VideoId = originalJob.VideoId,
                Parameters = originalParameters
            }),
            FailedAt = DateTime.UtcNow,
            AttemptedRetries = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _deadLetterRepository.AddAsync(deadLetterJob);
        await _unitOfWork.SaveChangesAsync();

        // Act - Deserialize and use original parameters for new job
        var payload = JsonSerializer.Deserialize<JsonElement>(deadLetterJob.OriginalPayload!);
        var savedParameters = payload.GetProperty("Parameters").GetRawText();

        var newJob = new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = originalJob.VideoId,
            UserId = originalJob.UserId,
            Type = originalJob.Type,
            Status = JobStatus.Pending,
            Parameters = savedParameters,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _jobRepository.AddAsync(newJob);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var restoredParams = JsonSerializer.Deserialize<JsonElement>(newJob.Parameters!);
        restoredParams.GetProperty("WhisperModel").GetString().Should().Be("medium");
        restoredParams.GetProperty("Language").GetString().Should().Be("es");
        restoredParams.GetProperty("TranslateToEnglish").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task DeadLetterQueue_GetStatistics_ShouldReturnCorrectCounts()
    {
        // Arrange
        await AuthenticateAsync();
        var video1 = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        var video2 = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        var video3 = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddRangeAsync(video1, video2, video3);

        var job1 = new Job { Id = Guid.NewGuid().ToString(), VideoId = video1.Id, UserId = video1.UserId, Type = JobType.Transcription, Status = JobStatus.Failed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var job2 = new Job { Id = Guid.NewGuid().ToString(), VideoId = video2.Id, UserId = video2.UserId, Type = JobType.Transcription, Status = JobStatus.Failed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var job3 = new Job { Id = Guid.NewGuid().ToString(), VideoId = video3.Id, UserId = video3.UserId, Type = JobType.Transcription, Status = JobStatus.Failed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await DbContext.Jobs.AddRangeAsync(job1, job2, job3);

        // Create DLQ entries with different failure reasons
        var dlq1 = new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = job1.Id, FailureReason = "MaxRetriesExceeded", FailedAt = DateTime.UtcNow, AttemptedRetries = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var dlq2 = new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = job2.Id, FailureReason = "MaxRetriesExceeded", FailedAt = DateTime.UtcNow, AttemptedRetries = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var dlq3 = new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = job3.Id, FailureReason = "PermanentError", FailedAt = DateTime.UtcNow, AttemptedRetries = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await DbContext.DeadLetterJobs.AddRangeAsync(dlq1, dlq2, dlq3);
        await DbContext.SaveChangesAsync();

        // Act
        var statistics = await _deadLetterRepository.GetFailureReasonStatisticsAsync();

        // Assert - Note: Statistics are global, may include data from other tests
        statistics.Should().NotBeNull();
        statistics.Should().ContainKey("MaxRetriesExceeded");
        statistics["MaxRetriesExceeded"].Should().BeGreaterThanOrEqualTo(2, "Should include at least the 2 entries created in this test");
        statistics.Should().ContainKey("PermanentError");
        statistics["PermanentError"].Should().BeGreaterThanOrEqualTo(1, "Should include at least the 1 entry created in this test");
    }

    [Fact]
    public async Task DeadLetterQueue_GetByFailureReason_ShouldFilterCorrectly()
    {
        // Arrange
        await AuthenticateAsync();
        var videos = Enumerable.Range(0, 5).Select(_ => TestDataGenerator.GenerateVideo(AuthenticatedUserId)).ToList();
        await DbContext.Videos.AddRangeAsync(videos);

        var jobs = videos.Select(v => new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = v.Id,
            UserId = v.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
        await DbContext.Jobs.AddRangeAsync(jobs);

        // 3 with MaxRetriesExceeded, 2 with PermanentError
        var dlqEntries = new List<DeadLetterJob>
        {
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[0].Id, FailureReason = "MaxRetriesExceeded", FailedAt = DateTime.UtcNow, AttemptedRetries = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[1].Id, FailureReason = "MaxRetriesExceeded", FailedAt = DateTime.UtcNow, AttemptedRetries = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[2].Id, FailureReason = "MaxRetriesExceeded", FailedAt = DateTime.UtcNow, AttemptedRetries = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[3].Id, FailureReason = "PermanentError", FailedAt = DateTime.UtcNow, AttemptedRetries = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[4].Id, FailureReason = "PermanentError", FailedAt = DateTime.UtcNow, AttemptedRetries = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        await DbContext.DeadLetterJobs.AddRangeAsync(dlqEntries);
        await DbContext.SaveChangesAsync();

        // Act
        var maxRetriesExceeded = await _deadLetterRepository.GetByFailureReasonAsync("MaxRetriesExceeded");
        var permanentErrors = await _deadLetterRepository.GetByFailureReasonAsync("PermanentError");

        // Assert
        maxRetriesExceeded.Should().HaveCount(3);
        maxRetriesExceeded.All(d => d.FailureReason == "MaxRetriesExceeded").Should().BeTrue();
        permanentErrors.Should().HaveCount(2);
        permanentErrors.All(d => d.FailureReason == "PermanentError").Should().BeTrue();
    }

    #endregion

    #region Query and Filtering Tests

    [Fact]
    public async Task DeadLetterQueue_GetWithRelatedData_ExcludesRequeuedByDefault()
    {
        // Arrange
        await AuthenticateAsync();
        var videos = Enumerable.Range(0, 3).Select(_ => TestDataGenerator.GenerateVideo(AuthenticatedUserId)).ToList();
        await DbContext.Videos.AddRangeAsync(videos);

        var jobs = videos.Select(v => new Job
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = v.Id,
            UserId = v.UserId,
            Type = JobType.Transcription,
            Status = JobStatus.Failed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
        await DbContext.Jobs.AddRangeAsync(jobs);

        var dlqEntries = new List<DeadLetterJob>
        {
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[0].Id, FailureReason = "MaxRetriesExceeded", IsRequeued = false, FailedAt = DateTime.UtcNow, AttemptedRetries = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[1].Id, FailureReason = "MaxRetriesExceeded", IsRequeued = true, RequeuedAt = DateTime.UtcNow, FailedAt = DateTime.UtcNow, AttemptedRetries = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = jobs[2].Id, FailureReason = "PermanentError", IsRequeued = false, FailedAt = DateTime.UtcNow, AttemptedRetries = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        await DbContext.DeadLetterJobs.AddRangeAsync(dlqEntries);
        await DbContext.SaveChangesAsync();

        // Act
        var withoutRequeued = await _deadLetterRepository.GetWithRelatedDataAsync(includeRequeued: false);
        var withRequeued = await _deadLetterRepository.GetWithRelatedDataAsync(includeRequeued: true);

        // Assert
        withoutRequeued.Should().HaveCount(2);
        withoutRequeued.All(d => !d.IsRequeued).Should().BeTrue();
        withRequeued.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeadLetterQueue_GetByDateRange_FiltersCorrectly()
    {
        // Arrange
        await AuthenticateAsync();
        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        await DbContext.Videos.AddAsync(video);

        var job1 = new Job { Id = Guid.NewGuid().ToString(), VideoId = video.Id, UserId = video.UserId, Type = JobType.Transcription, Status = JobStatus.Failed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var job2 = new Job { Id = Guid.NewGuid().ToString(), VideoId = video.Id, UserId = video.UserId, Type = JobType.Transcription, Status = JobStatus.Failed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var job3 = new Job { Id = Guid.NewGuid().ToString(), VideoId = video.Id, UserId = video.UserId, Type = JobType.Transcription, Status = JobStatus.Failed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await DbContext.Jobs.AddRangeAsync(job1, job2, job3);

        var yesterday = DateTime.UtcNow.AddDays(-1);
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        var tomorrow = DateTime.UtcNow.AddDays(1);

        var dlqEntries = new List<DeadLetterJob>
        {
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = job1.Id, FailureReason = "MaxRetriesExceeded", FailedAt = twoDaysAgo, AttemptedRetries = 5, CreatedAt = twoDaysAgo, UpdatedAt = twoDaysAgo },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = job2.Id, FailureReason = "MaxRetriesExceeded", FailedAt = yesterday, AttemptedRetries = 5, CreatedAt = yesterday, UpdatedAt = yesterday },
            new DeadLetterJob { Id = Guid.NewGuid().ToString(), JobId = job3.Id, FailureReason = "PermanentError", FailedAt = DateTime.UtcNow, AttemptedRetries = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        await DbContext.DeadLetterJobs.AddRangeAsync(dlqEntries);
        await DbContext.SaveChangesAsync();

        // Act
        var recentFailures = await _deadLetterRepository.GetByDateRangeAsync(yesterday.AddHours(-1), tomorrow);

        // Assert - Filter to only check entries created in this test
        var testEntries = recentFailures.Where(d => d.JobId == job2.Id || d.JobId == job3.Id).ToList();
        testEntries.Should().HaveCount(2, "Should find the 2 entries created in this test (yesterday and today)");
        testEntries.Should().NotContain(d => d.FailedAt < yesterday.AddHours(-1), "Should not include entries before the start date");
    }

    #endregion
}
