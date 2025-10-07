using Bogus;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Application.DTOs.Auth;
using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Tests.Integration.Helpers;

/// <summary>
/// Helper class to generate test data using Bogus
/// </summary>
public static class TestDataGenerator
{
    private static readonly Faker _faker = new Faker();

    /// <summary>
    /// Generates a fake user entity
    /// </summary>
    public static User GenerateUser(string? id = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Email = _faker.Internet.Email(),
            Name = _faker.Person.FullName,
            PasswordHash = _faker.Random.AlphaNumeric(60),
            IsActive = _faker.Random.Bool(),
            CreatedAt = _faker.Date.Past(),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a fake video entity
    /// </summary>
    public static Video GenerateVideo(string? userId = null, string? id = null)
    {
        var video = new Video
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            Title = _faker.Lorem.Sentence(),
            Description = _faker.Lorem.Paragraph(),
            Url = $"https://www.youtube.com/watch?v={_faker.Random.AlphaNumeric(11)}",
            YouTubeId = _faker.Random.AlphaNumeric(11),
            Duration = TimeSpan.FromSeconds(_faker.Random.Int(60, 7200)),
            ThumbnailUrl = _faker.Image.PicsumUrl(),
            Status = _faker.PickRandom<VideoStatus>(),
            CreatedAt = _faker.Date.Past(),
            UpdatedAt = DateTime.UtcNow,
            ViewCount = _faker.Random.Int(0, 1000000),
            LikeCount = _faker.Random.Int(0, 100000),
            ProcessingProgress = _faker.Random.Int(0, 100)
        };

        // Generate transcript segments if completed
        if (video.Status == VideoStatus.Completed)
        {
            video.TranscriptSegments = GenerateTranscriptSegments(video.Id, _faker.Random.Int(5, 20));
        }

        return video;
    }

    /// <summary>
    /// Generates fake transcript segments
    /// </summary>
    public static List<TranscriptSegment> GenerateTranscriptSegments(string videoId, int count = 10)
    {
        var segments = new List<TranscriptSegment>();
        float currentTime = 0;

        for (int i = 0; i < count; i++)
        {
            var segment = new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                StartTime = currentTime,
                EndTime = currentTime + (float)_faker.Random.Double(1, 5),
                Text = _faker.Lorem.Sentence(),
                CreatedAt = DateTime.UtcNow
            };

            currentTime = (float)segment.EndTime;
            segments.Add(segment);
        }

        return segments;
    }

    /// <summary>
    /// Generates a fake job entity
    /// </summary>
    public static Job GenerateJob(string? userId = null, string? videoId = null)
    {
        return new Job
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            VideoId = videoId,
            Type = _faker.PickRandom<JobType>(),
            Status = _faker.PickRandom<JobStatus>(),
            Progress = _faker.Random.Int(0, 100),
            StartedAt = _faker.Date.Recent(),
            CompletedAt = _faker.Random.Bool() ? _faker.Date.Recent() : null,
            ErrorMessage = _faker.Random.Bool() ? _faker.Lorem.Sentence() : null,
            CreatedAt = _faker.Date.Past(),
            UpdatedAt = DateTime.UtcNow,
            RetryCount = _faker.Random.Int(0, 3),
            MaxRetries = 3
        };
    }

    /// <summary>
    /// Generates a register request DTO
    /// </summary>
    public static RegisterRequestDto GenerateRegisterRequest()
    {
        return new RegisterRequestDto
        {
            Email = _faker.Internet.Email(),
            Password = "Test123!@#",
            Name = _faker.Person.FullName
        };
    }

    /// <summary>
    /// Generates a login request DTO
    /// </summary>
    public static LoginRequestDto GenerateLoginRequest()
    {
        return new LoginRequestDto
        {
            Email = _faker.Internet.Email(),
            Password = "Test123!@#"
        };
    }

    /// <summary>
    /// Generates a video creation DTO
    /// </summary>
    public static CreateVideoDto GenerateCreateVideoDto()
    {
        return new CreateVideoDto
        {
            Title = _faker.Lorem.Sentence(),
            Description = _faker.Lorem.Paragraph(),
            YoutubeUrl = $"https://www.youtube.com/watch?v={_faker.Random.AlphaNumeric(11)}",
            ThumbnailUrl = _faker.Image.PicsumUrl(),
            AutoProcess = _faker.Random.Bool(),
            Language = _faker.PickRandom("en", "es", "fr", "de")
        };
    }

    /// <summary>
    /// Generates a video update DTO
    /// </summary>
    public static UpdateVideoDto GenerateUpdateVideoDto()
    {
        return new UpdateVideoDto
        {
            Title = _faker.Lorem.Sentence(),
            Description = _faker.Lorem.Paragraph()
        };
    }

}