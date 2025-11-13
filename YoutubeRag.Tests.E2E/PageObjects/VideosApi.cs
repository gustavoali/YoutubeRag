using Microsoft.Playwright;

namespace YoutubeRag.Tests.E2E.PageObjects;

/// <summary>
/// Page Object for Videos API endpoints
/// </summary>
public class VideosApi : ApiClient
{
    public VideosApi(IAPIRequestContext requestContext, string baseUrl)
        : base(requestContext, baseUrl)
    {
    }

    /// <summary>
    /// Ingest video from YouTube URL
    /// </summary>
    public async Task<IAPIResponse> IngestVideoAsync(string url, string? title = null, string? description = null, int priority = 0)
    {
        var data = new
        {
            url,
            title,
            description,
            priority
        };

        return await PostAsync("/videos/ingest", data);
    }

    /// <summary>
    /// Submit video from YouTube URL (US-101 endpoint)
    /// </summary>
    public async Task<IAPIResponse> SubmitVideoFromUrlAsync(string url)
    {
        var data = new
        {
            url
        };

        return await PostAsync("/videos/from-url", data);
    }

    /// <summary>
    /// Process video from URL (legacy endpoint)
    /// </summary>
    public async Task<IAPIResponse> ProcessVideoFromUrlAsync(string url, string? title = null, string? description = null)
    {
        var data = new
        {
            url,
            title,
            description
        };

        return await PostAsync("/videos/from-url", data);
    }

    /// <summary>
    /// Get list of videos
    /// </summary>
    public async Task<IAPIResponse> GetVideosAsync(int page = 1, int pageSize = 20)
    {
        return await GetAsync($"/videos?page={page}&pageSize={pageSize}");
    }

    /// <summary>
    /// Get video by ID
    /// </summary>
    public async Task<IAPIResponse> GetVideoByIdAsync(string videoId)
    {
        return await GetAsync($"/videos/{videoId}");
    }

    /// <summary>
    /// Get video processing progress
    /// </summary>
    public async Task<IAPIResponse> GetVideoProgressAsync(string videoId)
    {
        return await GetAsync($"/videos/{videoId}/progress");
    }

    /// <summary>
    /// Update video metadata
    /// </summary>
    public async Task<IAPIResponse> UpdateVideoAsync(string videoId, string? title = null, string? description = null)
    {
        var data = new
        {
            title,
            description
        };

        return await PutAsync($"/videos/{videoId}", data);
    }

    /// <summary>
    /// Delete video
    /// </summary>
    public async Task<IAPIResponse> DeleteVideoAsync(string videoId)
    {
        return await DeleteAsync($"/videos/{videoId}");
    }

    /// <summary>
    /// Get video transcript
    /// </summary>
    public async Task<IAPIResponse> GetVideoTranscriptAsync(string videoId)
    {
        return await GetAsync($"/videos/{videoId}/transcript");
    }

    /// <summary>
    /// Reprocess video
    /// </summary>
    public async Task<IAPIResponse> ReprocessVideoAsync(string videoId, bool extractAudio = true, bool generateTranscript = true)
    {
        var data = new
        {
            extractAudio,
            generateTranscript,
            generateEmbeddings = true
        };

        return await PostAsync($"/videos/{videoId}/reprocess", data);
    }
}
