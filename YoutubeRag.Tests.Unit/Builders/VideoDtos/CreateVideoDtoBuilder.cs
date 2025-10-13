using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Tests.Unit.Builders.VideoDtos;

/// <summary>
/// Builder for creating CreateVideoDto test instances
/// </summary>
public class CreateVideoDtoBuilder
{
    private string _title = "Test Video";
    private string? _description = "Test Description";
    private string? _youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    private string? _thumbnailUrl = "https://example.com/thumbnail.jpg";
    private string? _metadata = null;
    private bool _autoProcess = false;
    private string? _language = "en";

    public CreateVideoDtoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateVideoDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public CreateVideoDtoBuilder WithYoutubeUrl(string? youtubeUrl)
    {
        _youtubeUrl = youtubeUrl;
        return this;
    }

    public CreateVideoDtoBuilder WithThumbnailUrl(string? thumbnailUrl)
    {
        _thumbnailUrl = thumbnailUrl;
        return this;
    }

    public CreateVideoDtoBuilder WithMetadata(string? metadata)
    {
        _metadata = metadata;
        return this;
    }

    public CreateVideoDtoBuilder WithAutoProcess(bool autoProcess = true)
    {
        _autoProcess = autoProcess;
        return this;
    }

    public CreateVideoDtoBuilder WithLanguage(string? language)
    {
        _language = language;
        return this;
    }

    public CreateVideoDto Build()
    {
        return new CreateVideoDto
        {
            Title = _title,
            Description = _description,
            YoutubeUrl = _youtubeUrl,
            ThumbnailUrl = _thumbnailUrl,
            Metadata = _metadata,
            AutoProcess = _autoProcess,
            Language = _language
        };
    }

    /// <summary>
    /// Creates a valid CreateVideoDto with default values
    /// </summary>
    public static CreateVideoDto CreateValid() => new CreateVideoDtoBuilder().Build();

    /// <summary>
    /// Creates a CreateVideoDto with empty title (validation error)
    /// </summary>
    public static CreateVideoDto CreateWithEmptyTitle() =>
        new CreateVideoDtoBuilder().WithTitle(string.Empty).Build();

    /// <summary>
    /// Creates a CreateVideoDto without YouTube URL
    /// </summary>
    public static CreateVideoDto CreateWithoutUrl() =>
        new CreateVideoDtoBuilder().WithYoutubeUrl(null).Build();

    /// <summary>
    /// Creates a CreateVideoDto with auto-processing enabled
    /// </summary>
    public static CreateVideoDto CreateWithAutoProcess() =>
        new CreateVideoDtoBuilder().WithAutoProcess(true).Build();
}
