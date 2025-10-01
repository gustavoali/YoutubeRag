using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;
using YoutubeRag.Tests.Integration.Helpers;

namespace YoutubeRag.Tests.Integration.Fixtures;

/// <summary>
/// Database seeder for integration tests
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with comprehensive test data
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Clear existing data
        await ClearDatabaseAsync(context);

        // Seed users
        var users = await SeedUsersAsync(context);

        // Seed videos for each user
        var videos = await SeedVideosAsync(context, users);

        // Seed jobs
        await SeedJobsAsync(context, users, videos);

        // Seed transcript segments
        await SeedTranscriptsAsync(context, videos);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Clears all data from the database
    /// </summary>
    private static async Task ClearDatabaseAsync(ApplicationDbContext context)
    {
        context.TranscriptSegments.RemoveRange(context.TranscriptSegments);
        context.Jobs.RemoveRange(context.Jobs);
        context.Videos.RemoveRange(context.Videos);
        context.Users.RemoveRange(context.Users);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds test users
    /// </summary>
    private static async Task<List<User>> SeedUsersAsync(ApplicationDbContext context)
    {
        var users = new List<User>
        {
            new User
            {
                Id = "user-1",
                Email = "alice@example.com",
                Name = "Alice Johnson",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = "user-2",
                Email = "bob@example.com",
                Name = "Bob Smith",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = "user-3",
                Email = "charlie@example.com",
                Name = "Charlie Brown",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                IsActive = false, // Inactive user
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);
        return users;
    }

    /// <summary>
    /// Seeds test videos
    /// </summary>
    private static async Task<List<Video>> SeedVideosAsync(ApplicationDbContext context, List<User> users)
    {
        var videos = new List<Video>();

        // Videos for user 1
        videos.AddRange(new[]
        {
            new Video
            {
                Id = "video-1-1",
                UserId = users[0].Id,
                Title = "Introduction to C# Programming",
                Description = "Learn the basics of C# programming language",
                YoutubeUrl = "https://www.youtube.com/watch?v=abc123",
                YoutubeId = "abc123",
                Duration = TimeSpan.FromSeconds(1800),
                ThumbnailUrl = "https://img.youtube.com/vi/abc123/0.jpg",
                Status = VideoStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-29),
                ViewCount = 1000,
                LikeCount = 50,
                ProcessingProgress = 100
            },
            new Video
            {
                Id = "video-1-2",
                UserId = users[0].Id,
                Title = "Advanced ASP.NET Core Development",
                Description = "Deep dive into ASP.NET Core features",
                YoutubeUrl = "https://www.youtube.com/watch?v=def456",
                YoutubeId = "def456",
                Duration = TimeSpan.FromSeconds(3600),
                ThumbnailUrl = "https://img.youtube.com/vi/def456/0.jpg",
                Status = VideoStatus.Processing,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow,
                ViewCount = 500,
                LikeCount = 25,
                ProcessingProgress = 45
            },
            new Video
            {
                Id = "video-1-3",
                UserId = users[0].Id,
                Title = "Entity Framework Core Tutorial",
                Description = "Complete guide to Entity Framework Core",
                YoutubeUrl = "https://www.youtube.com/watch?v=ghi789",
                YoutubeId = "ghi789",
                Duration = TimeSpan.FromSeconds(2400),
                ThumbnailUrl = "https://img.youtube.com/vi/ghi789/0.jpg",
                Status = VideoStatus.Failed,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-6),
                ViewCount = 200,
                LikeCount = 10,
                ProcessingProgress = 0,
                ErrorMessage = "Failed to process video: Invalid format"
            }
        });

        // Videos for user 2
        videos.AddRange(new[]
        {
            new Video
            {
                Id = "video-2-1",
                UserId = users[1].Id,
                Title = "Machine Learning Fundamentals",
                Description = "Introduction to ML concepts and algorithms",
                YoutubeUrl = "https://www.youtube.com/watch?v=ml001",
                YoutubeId = "ml001",
                Duration = TimeSpan.FromSeconds(2700),
                ThumbnailUrl = "https://img.youtube.com/vi/ml001/0.jpg",
                Status = VideoStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-19),
                ViewCount = 1500,
                LikeCount = 75,
                ProcessingProgress = 100
            },
            new Video
            {
                Id = "video-2-2",
                UserId = users[1].Id,
                Title = "Deep Learning with PyTorch",
                Description = "Building neural networks with PyTorch",
                YoutubeUrl = "https://www.youtube.com/watch?v=ml002",
                YoutubeId = "ml002",
                Duration = TimeSpan.FromSeconds(4200),
                ThumbnailUrl = "https://img.youtube.com/vi/ml002/0.jpg",
                Status = VideoStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                ViewCount = 100,
                LikeCount = 5,
                ProcessingProgress = 0
            }
        });

        await context.Videos.AddRangeAsync(videos);
        return videos;
    }

    /// <summary>
    /// Seeds test jobs
    /// </summary>
    private static async Task SeedJobsAsync(ApplicationDbContext context, List<User> users, List<Video> videos)
    {
        var jobs = new List<Job>
        {
            new Job
            {
                Id = "job-1",
                UserId = users[0].Id,
                VideoId = videos[0].Id,
                JobType = "VideoProcessing",
                Status = JobStatus.Completed,
                Progress = 100,
                StartedAt = DateTime.UtcNow.AddDays(-29),
                CompletedAt = DateTime.UtcNow.AddDays(-29).AddHours(1),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-29),
                RetryCount = 0,
                MaxRetries = 3
            },
            new Job
            {
                Id = "job-2",
                UserId = users[0].Id,
                VideoId = videos[1].Id,
                JobType = "VideoProcessing",
                Status = JobStatus.Running,
                Progress = 45,
                StartedAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow,
                RetryCount = 0,
                MaxRetries = 3
            },
            new Job
            {
                Id = "job-3",
                UserId = users[0].Id,
                VideoId = videos[2].Id,
                JobType = "VideoProcessing",
                Status = JobStatus.Failed,
                Progress = 0,
                StartedAt = DateTime.UtcNow.AddDays(-6),
                CompletedAt = DateTime.UtcNow.AddDays(-6).AddMinutes(5),
                ErrorMessage = "Failed to process video: Invalid format",
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-6),
                RetryCount = 3,
                MaxRetries = 3
            },
            new Job
            {
                Id = "job-4",
                UserId = users[1].Id,
                VideoId = videos[3].Id,
                JobType = "VideoProcessing",
                Status = JobStatus.Completed,
                Progress = 100,
                StartedAt = DateTime.UtcNow.AddDays(-19),
                CompletedAt = DateTime.UtcNow.AddDays(-19).AddHours(2),
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-19),
                RetryCount = 0,
                MaxRetries = 3
            }
        };

        await context.Jobs.AddRangeAsync(jobs);
    }

    /// <summary>
    /// Seeds test transcript segments
    /// </summary>
    private static async Task SeedTranscriptsAsync(ApplicationDbContext context, List<Video> videos)
    {
        var transcripts = new List<TranscriptSegment>();

        // Add transcripts for processed videos
        var processedVideos = videos.Where(v => v.Status == VideoStatus.Completed).ToList();

        foreach (var video in processedVideos)
        {
            if (video.Id == "video-1-1")
            {
                transcripts.AddRange(new[]
                {
                    new TranscriptSegment
                    {
                        Id = $"{video.Id}-seg-1",
                        VideoId = video.Id,
                        StartTime = 0,
                        EndTime = 10,
                        Text = "Welcome to this comprehensive C# programming tutorial.",
                        CreatedAt = DateTime.UtcNow.AddDays(-29)
                    },
                    new TranscriptSegment
                    {
                        Id = $"{video.Id}-seg-2",
                        VideoId = video.Id,
                        StartTime = 10,
                        EndTime = 25,
                        Text = "C# is a modern, object-oriented programming language developed by Microsoft.",
                        CreatedAt = DateTime.UtcNow.AddDays(-29)
                    },
                    new TranscriptSegment
                    {
                        Id = $"{video.Id}-seg-3",
                        VideoId = video.Id,
                        StartTime = 25,
                        EndTime = 40,
                        Text = "In this tutorial, we'll cover variables, data types, and control structures.",
                        CreatedAt = DateTime.UtcNow.AddDays(-29)
                    }
                });
            }
            else if (video.Id == "video-2-1")
            {
                transcripts.AddRange(new[]
                {
                    new TranscriptSegment
                    {
                        Id = $"{video.Id}-seg-1",
                        VideoId = video.Id,
                        StartTime = 0,
                        EndTime = 15,
                        Text = "Machine learning is a subset of artificial intelligence that enables computers to learn from data.",
                        CreatedAt = DateTime.UtcNow.AddDays(-19)
                    },
                    new TranscriptSegment
                    {
                        Id = $"{video.Id}-seg-2",
                        VideoId = video.Id,
                        StartTime = 15,
                        EndTime = 30,
                        Text = "We'll explore supervised learning, unsupervised learning, and reinforcement learning.",
                        CreatedAt = DateTime.UtcNow.AddDays(-19)
                    },
                    new TranscriptSegment
                    {
                        Id = $"{video.Id}-seg-3",
                        VideoId = video.Id,
                        StartTime = 30,
                        EndTime = 45,
                        Text = "Popular algorithms include linear regression, decision trees, and neural networks.",
                        CreatedAt = DateTime.UtcNow.AddDays(-19)
                    }
                });
            }
        }

        await context.TranscriptSegments.AddRangeAsync(transcripts);
    }
}