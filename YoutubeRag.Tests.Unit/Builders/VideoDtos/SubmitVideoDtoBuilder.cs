using YoutubeRag.Application.DTOs.Video;

namespace YoutubeRag.Tests.Unit.Builders.VideoDtos;

/// <summary>
/// Builder for creating SubmitVideoDto test instances
/// </summary>
public class SubmitVideoDtoBuilder
{
    private string _url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

    public SubmitVideoDtoBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    public SubmitVideoDto Build()
    {
        return new SubmitVideoDto
        {
            Url = _url
        };
    }

    /// <summary>
    /// Creates a valid SubmitVideoDto with youtube.com format
    /// </summary>
    public static SubmitVideoDto CreateValidYouTubeCom() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("https://www.youtube.com/watch?v=dQw4w9WgXcQ")
            .Build();

    /// <summary>
    /// Creates a valid SubmitVideoDto with youtu.be format
    /// </summary>
    public static SubmitVideoDto CreateValidYouTuBe() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("https://youtu.be/dQw4w9WgXcQ")
            .Build();

    /// <summary>
    /// Creates a valid SubmitVideoDto with embed format
    /// </summary>
    public static SubmitVideoDto CreateValidEmbed() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("https://www.youtube.com/embed/dQw4w9WgXcQ")
            .Build();

    /// <summary>
    /// Creates a valid SubmitVideoDto with /v/ format
    /// </summary>
    public static SubmitVideoDto CreateValidV() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("https://www.youtube.com/v/dQw4w9WgXcQ")
            .Build();

    /// <summary>
    /// Creates a SubmitVideoDto with empty URL
    /// </summary>
    public static SubmitVideoDto CreateWithEmptyUrl() =>
        new SubmitVideoDtoBuilder()
            .WithUrl(string.Empty)
            .Build();

    /// <summary>
    /// Creates a SubmitVideoDto with whitespace URL
    /// </summary>
    public static SubmitVideoDto CreateWithWhitespaceUrl() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("   ")
            .Build();

    /// <summary>
    /// Creates a SubmitVideoDto with non-YouTube URL
    /// </summary>
    public static SubmitVideoDto CreateWithNonYouTubeUrl() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("https://vimeo.com/123456")
            .Build();

    /// <summary>
    /// Creates a SubmitVideoDto with invalid format
    /// </summary>
    public static SubmitVideoDto CreateWithInvalidFormat() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("not-a-url")
            .Build();

    /// <summary>
    /// Creates a SubmitVideoDto with URL exceeding max length (2048 chars)
    /// </summary>
    public static SubmitVideoDto CreateWithExcessivelyLongUrl()
    {
        var longUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&" + new string('a', 2100);
        return new SubmitVideoDtoBuilder()
            .WithUrl(longUrl)
            .Build();
    }

    /// <summary>
    /// Creates a SubmitVideoDto with URL with additional query parameters
    /// </summary>
    public static SubmitVideoDto CreateWithQueryParameters() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s&list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")
            .Build();

    /// <summary>
    /// Creates a SubmitVideoDto with youtu.be URL with query parameters
    /// </summary>
    public static SubmitVideoDto CreateYouTuBeWithQueryParameters() =>
        new SubmitVideoDtoBuilder()
            .WithUrl("https://youtu.be/dQw4w9WgXcQ?t=30s")
            .Build();
}
