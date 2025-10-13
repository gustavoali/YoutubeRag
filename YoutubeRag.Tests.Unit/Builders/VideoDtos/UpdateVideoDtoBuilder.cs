using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Tests.Unit.Builders.VideoDtos;

/// <summary>
/// Builder for creating UpdateVideoDto test instances
/// </summary>
public class UpdateVideoDtoBuilder
{
    private string? _title = null;
    private string? _description = null;
    private string? _thumbnailUrl = null;
    private string? _metadata = null;
    private bool? _clearDescription = null;
    private bool? _clearThumbnail = null;
    private VideoStatus? _status = null;
    private TimeSpan? _duration = null;

    public UpdateVideoDtoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public UpdateVideoDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public UpdateVideoDtoBuilder WithThumbnailUrl(string thumbnailUrl)
    {
        _thumbnailUrl = thumbnailUrl;
        return this;
    }

    public UpdateVideoDtoBuilder WithMetadata(string metadata)
    {
        _metadata = metadata;
        return this;
    }

    public UpdateVideoDtoBuilder WithClearDescription(bool clearDescription = true)
    {
        _clearDescription = clearDescription;
        return this;
    }

    public UpdateVideoDtoBuilder WithClearThumbnail(bool clearThumbnail = true)
    {
        _clearThumbnail = clearThumbnail;
        return this;
    }

    public UpdateVideoDtoBuilder WithStatus(VideoStatus status)
    {
        _status = status;
        return this;
    }

    public UpdateVideoDtoBuilder WithDuration(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    public UpdateVideoDto Build()
    {
        return new UpdateVideoDto
        {
            Title = _title,
            Description = _description,
            ThumbnailUrl = _thumbnailUrl,
            Metadata = _metadata,
            ClearDescription = _clearDescription,
            ClearThumbnail = _clearThumbnail,
            Status = _status,
            Duration = _duration
        };
    }

    /// <summary>
    /// Creates an UpdateVideoDto with title update
    /// </summary>
    public static UpdateVideoDto CreateWithTitleUpdate() =>
        new UpdateVideoDtoBuilder().WithTitle("Updated Title").Build();

    /// <summary>
    /// Creates an UpdateVideoDto with status update
    /// </summary>
    public static UpdateVideoDto CreateWithStatusUpdate(VideoStatus status) =>
        new UpdateVideoDtoBuilder().WithStatus(status).Build();

    /// <summary>
    /// Creates an UpdateVideoDto with all fields
    /// </summary>
    public static UpdateVideoDto CreateWithAllFields() =>
        new UpdateVideoDtoBuilder()
            .WithTitle("Updated Title")
            .WithDescription("Updated Description")
            .WithStatus(VideoStatus.Completed)
            .WithDuration(TimeSpan.FromMinutes(10))
            .Build();

    /// <summary>
    /// Creates an empty UpdateVideoDto (no fields to update)
    /// </summary>
    public static UpdateVideoDto CreateEmpty() =>
        new UpdateVideoDtoBuilder().Build();
}
