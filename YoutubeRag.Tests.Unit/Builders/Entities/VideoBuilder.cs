using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Tests.Unit.Builders.Entities;

/// <summary>
/// Builder for creating Video test instances
/// </summary>
public class VideoBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string? _youtubeId = "dQw4w9WgXcQ";
    private string _title = "Test Video";
    private string? _description = "Test Description";
    private string? _url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    private TimeSpan? _duration = TimeSpan.FromMinutes(5);
    private VideoStatus _status = VideoStatus.Pending;
    private string _userId = Guid.NewGuid().ToString();
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _updatedAt = DateTime.UtcNow;
    private string? _audioPath = null;
    private string? _thumbnailUrl = null;

    public VideoBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public VideoBuilder WithYouTubeId(string? youtubeId)
    {
        _youtubeId = youtubeId;
        return this;
    }

    public VideoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public VideoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public VideoBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    public VideoBuilder WithDuration(TimeSpan? duration)
    {
        _duration = duration;
        return this;
    }

    public VideoBuilder WithStatus(VideoStatus status)
    {
        _status = status;
        return this;
    }

    public VideoBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public VideoBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public VideoBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public VideoBuilder WithAudioPath(string audioPath)
    {
        _audioPath = audioPath;
        return this;
    }

    public VideoBuilder WithThumbnailUrl(string? thumbnailUrl)
    {
        _thumbnailUrl = thumbnailUrl;
        return this;
    }

    public Video Build()
    {
        return new Video
        {
            Id = _id,
            YouTubeId = _youtubeId,
            Title = _title,
            Description = _description,
            Url = _url,
            Duration = _duration,
            Status = _status,
            UserId = _userId,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            AudioPath = _audioPath,
            ThumbnailUrl = _thumbnailUrl
        };
    }

    /// <summary>
    /// Creates a valid pending Video with default values
    /// </summary>
    public static Video CreateValid() => new VideoBuilder().Build();

    /// <summary>
    /// Creates a completed Video with all processing done
    /// </summary>
    public static Video CreateCompleted() =>
        new VideoBuilder()
            .WithStatus(VideoStatus.Completed)
            .WithAudioPath("/tmp/audio.mp3")
            .Build();

    /// <summary>
    /// Creates a failed Video
    /// </summary>
    public static Video CreateFailed() =>
        new VideoBuilder().WithStatus(VideoStatus.Failed).Build();

    /// <summary>
    /// Creates a Video in processing state
    /// </summary>
    public static Video CreateProcessing() =>
        new VideoBuilder().WithStatus(VideoStatus.Processing).Build();
}
