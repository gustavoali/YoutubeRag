using YoutubeRag.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;

namespace YoutubeRag.Infrastructure.Services;

public class LocalEmbeddingService : IEmbeddingService
{
    private readonly ILogger<LocalEmbeddingService> _logger;

    public LocalEmbeddingService(ILogger<LocalEmbeddingService> logger)
    {
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        _logger.LogDebug("Local Embeddings: Generating embedding for text of length {Length}", text.Length);

        await Task.Delay(50); // Simulate processing time

        // Generate deterministic embedding based on text content
        // This is a simplified approach - in production you'd use a real embedding model
        var embedding = GenerateDeterministicEmbedding(text);

        return embedding;
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        _logger.LogInformation("Local Embeddings: Generating {Count} embeddings", texts.Count);

        var embeddings = new List<float[]>();

        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text);
            embeddings.Add(embedding);
        }

        return embeddings;
    }

    public async Task<List<SearchResult>> SearchSimilarAsync(string query, int limit = 10, double threshold = 0.7)
    {
        _logger.LogInformation("Local Embeddings: Searching for similar content to query: {Query}",
            query.Substring(0, Math.Min(50, query.Length)));

        await Task.Delay(200); // Simulate search time

        // For local implementation, we'll use keyword-based similarity
        // In production, you'd load embeddings from database and calculate cosine similarity
        var mockResults = GenerateLocalSearchResults(query, limit, threshold);

        var filteredResults = mockResults
            .Where(r => r.Similarity >= threshold)
            .OrderByDescending(r => r.Similarity)
            .Take(limit)
            .ToList();

        _logger.LogInformation("Local Embeddings: Found {Count} similar segments above threshold {Threshold}",
            filteredResults.Count, threshold);

        return filteredResults;
    }

    public async Task<bool> IndexTranscriptSegmentsAsync(string videoId, List<string> segments)
    {
        _logger.LogInformation("Local Embeddings: Indexing {Count} transcript segments for video {VideoId}",
            segments.Count, videoId);

        // In a real implementation, you would:
        // 1. Generate embeddings for each segment
        // 2. Store them in a vector database or file system
        // 3. Create an index for fast similarity search

        await Task.Delay(segments.Count * 20); // Simulate processing time

        return true;
    }

    public async Task<bool> DeleteVideoEmbeddingsAsync(string videoId)
    {
        _logger.LogInformation("Local Embeddings: Deleting embeddings for video {VideoId}", videoId);

        await Task.Delay(100);

        return true;
    }

    private float[] GenerateDeterministicEmbedding(string text)
    {
        // Generate a deterministic 384-dimensional embedding based on text content
        // This is simplified - real embeddings would use trained models
        const int dimensions = 384; // Typical size for sentence transformers
        var embedding = new float[dimensions];

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

        // Use hash to seed random number generator for consistency
        var seed = BitConverter.ToInt32(hash, 0);
        var random = new Random(seed);

        // Generate embedding with some structure based on text features
        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;
        var textLength = text.Length;

        for (int i = 0; i < dimensions; i++)
        {
            var value = (float)random.NextDouble() * 2.0f - 1.0f; // -1 to 1

            // Add some structure based on text characteristics
            if (i < 10) // First 10 dimensions based on length
            {
                value += (float)Math.Sin(textLength * 0.01) * 0.3f;
            }
            else if (i < 20) // Next 10 based on word count
            {
                value += (float)Math.Cos(wordCount * 0.1) * 0.3f;
            }
            else if (i < 50) // Add some keyword-based features
            {
                foreach (var word in words.Take(5))
                {
                    value += (float)Math.Sin(word.GetHashCode() * 0.0001) * 0.1f;
                }
            }

            embedding[i] = value;
        }

        // Normalize to unit vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < dimensions; i++)
            {
                embedding[i] = (float)(embedding[i] / magnitude);
            }
        }

        return embedding;
    }

    private List<SearchResult> GenerateLocalSearchResults(string query, int limit, double threshold)
    {
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var mockSegments = new[]
        {
            new { Text = "Introduction to machine learning algorithms and their applications", Video = "AI Basics" },
            new { Text = "Deep learning neural networks for image recognition", Video = "Deep Learning Course" },
            new { Text = "Natural language processing with transformers and attention mechanisms", Video = "NLP Tutorial" },
            new { Text = "Computer vision techniques for object detection", Video = "CV Workshop" },
            new { Text = "Reinforcement learning in game playing and robotics", Video = "RL Fundamentals" },
            new { Text = "Data preprocessing and feature engineering best practices", Video = "Data Science 101" },
            new { Text = "Statistical analysis and hypothesis testing methods", Video = "Statistics Course" },
            new { Text = "Python programming for data analysis and visualization", Video = "Python Tutorial" },
            new { Text = "Database design and SQL query optimization", Video = "Database Course" },
            new { Text = "Web development with modern JavaScript frameworks", Video = "Web Dev Bootcamp" }
        };

        var results = new List<SearchResult>();

        foreach (var segment in mockSegments)
        {
            var segmentWords = segment.Text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchingWords = queryWords.Intersect(segmentWords).Count();
            var similarity = (double)matchingWords / Math.Max(queryWords.Length, segmentWords.Length);

            // Add some randomness based on text content for more realistic results
            var textHash = segment.Text.GetHashCode();
            var randomFactor = (textHash % 100) / 1000.0; // Small random component
            similarity += randomFactor;

            if (similarity >= threshold)
            {
                results.Add(new SearchResult
                {
                    VideoId = $"video_{Math.Abs(segment.Video.GetHashCode()) % 1000}",
                    SegmentId = $"seg_{Math.Abs(segment.Text.GetHashCode()) % 10000}",
                    Text = segment.Text,
                    StartTime = (textHash % 300) + 10, // Random start time
                    EndTime = (textHash % 300) + 20, // Random end time
                    Similarity = Math.Min(similarity, 1.0),
                    VideoTitle = segment.Video,
                    VideoThumbnail = $"https://img.youtube.com/vi/example_{Math.Abs(textHash) % 100}/maxresdefault.jpg"
                });
            }
        }

        return results;
    }
}