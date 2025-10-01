using YoutubeRag.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace YoutubeRag.Infrastructure.Services;

public class MockEmbeddingService : IEmbeddingService
{
    private readonly ILogger<MockEmbeddingService> _logger;
    private readonly Random _random = new Random();

    public MockEmbeddingService(ILogger<MockEmbeddingService> logger)
    {
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        _logger.LogDebug("Mock: Generating embedding for text of length {Length}", text.Length);

        await Task.Delay(200); // Simulate API call delay

        // Generate a mock embedding vector (1536 dimensions like OpenAI's text-embedding-3-small)
        var embedding = new float[1536];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(_random.NextDouble() * 2.0 - 1.0); // Random values between -1 and 1
        }

        // Normalize the vector to unit length
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(embedding[i] / magnitude);
        }

        return embedding;
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        _logger.LogInformation("Mock: Generating {Count} embeddings", texts.Count);

        var embeddings = new List<float[]>();

        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text);
            embeddings.Add(embedding);

            // Small delay between embeddings
            await Task.Delay(50);
        }

        return embeddings;
    }

    public async Task<List<SearchResult>> SearchSimilarAsync(string query, int limit = 10, double threshold = 0.7)
    {
        _logger.LogInformation("Mock: Searching for similar content to query: {Query}", query.Substring(0, Math.Min(50, query.Length)));

        await Task.Delay(800); // Simulate search time

        // Return mock search results
        var mockResults = new List<SearchResult>
        {
            new SearchResult
            {
                VideoId = "video-1",
                SegmentId = "segment-1",
                Text = "Welcome to this mock YouTube video transcription.",
                StartTime = 0.0,
                EndTime = 5.0,
                Similarity = 0.92,
                VideoTitle = "Mock Video - Sample YouTube Content",
                VideoThumbnail = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"
            },
            new SearchResult
            {
                VideoId = "video-1",
                SegmentId = "segment-2",
                Text = "Today we will be exploring the fascinating world of artificial intelligence and machine learning.",
                StartTime = 5.0,
                EndTime = 12.0,
                Similarity = 0.89,
                VideoTitle = "Mock Video - Sample YouTube Content",
                VideoThumbnail = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"
            },
            new SearchResult
            {
                VideoId = "video-2",
                SegmentId = "segment-3",
                Text = "This technology has revolutionized how we process information and understand natural language.",
                StartTime = 12.0,
                EndTime = 20.0,
                Similarity = 0.85,
                VideoTitle = "AI and Machine Learning Basics",
                VideoThumbnail = "https://img.youtube.com/vi/example/maxresdefault.jpg"
            },
            new SearchResult
            {
                VideoId = "video-2",
                SegmentId = "segment-4",
                Text = "Natural language processing allows computers to understand and generate human language.",
                StartTime = 25.0,
                EndTime = 32.0,
                Similarity = 0.82,
                VideoTitle = "AI and Machine Learning Basics",
                VideoThumbnail = "https://img.youtube.com/vi/example/maxresdefault.jpg"
            },
            new SearchResult
            {
                VideoId = "video-3",
                SegmentId = "segment-5",
                Text = "Machine learning models can be trained on large datasets to improve their performance.",
                StartTime = 45.0,
                EndTime = 52.0,
                Similarity = 0.78,
                VideoTitle = "Deep Learning Fundamentals",
                VideoThumbnail = "https://img.youtube.com/vi/example2/maxresdefault.jpg"
            }
        };

        // Filter by threshold and limit
        var filteredResults = mockResults
            .Where(r => r.Similarity >= threshold)
            .OrderByDescending(r => r.Similarity)
            .Take(limit)
            .ToList();

        _logger.LogInformation("Mock: Found {Count} similar segments above threshold {Threshold}",
            filteredResults.Count, threshold);

        return filteredResults;
    }

    public async Task<bool> IndexTranscriptSegmentsAsync(string videoId, List<string> segments)
    {
        _logger.LogInformation("Mock: Indexing {Count} transcript segments for video {VideoId}",
            segments.Count, videoId);

        // Simulate embedding generation and indexing time
        await Task.Delay(segments.Count * 100);

        return true;
    }

    public async Task<bool> DeleteVideoEmbeddingsAsync(string videoId)
    {
        _logger.LogInformation("Mock: Deleting embeddings for video {VideoId}", videoId);

        await Task.Delay(300);

        return true;
    }
}