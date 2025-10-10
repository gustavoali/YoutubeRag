using Microsoft.Extensions.Logging;
using System.Text.Json;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Local embedding service implementation for generating text embeddings
/// For MVP, this uses mock embeddings. In production, integrate with ONNX or Python embedding models
/// </summary>
public class LocalEmbeddingService : IEmbeddingService
{
    private readonly ILogger<LocalEmbeddingService> _logger;
    private readonly Random _random;
    private const int EMBEDDING_DIMENSION = 384; // Standard dimension for sentence-transformers/all-MiniLM-L6-v2
    private const int MAX_BATCH_SIZE = 32;

    public LocalEmbeddingService(ILogger<LocalEmbeddingService> logger)
    {
        _logger = logger;
        _random = new Random(42); // Fixed seed for reproducibility in MVP
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        try
        {
            _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

            // For MVP: Generate deterministic mock embeddings based on text content
            var embedding = GenerateMockEmbedding(text);

            await Task.Delay(10, cancellationToken); // Simulate processing time

            _logger.LogDebug("Successfully generated embedding of dimension {Dimension}", embedding.Length);
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<(string segmentId, float[] embedding)>> GenerateEmbeddingsAsync(
        List<(string segmentId, string text)> texts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts, nameof(texts));

        if (texts.Count == 0)
        {
            return new List<(string, float[])>();
        }

        _logger.LogInformation("Generating embeddings for {Count} texts", texts.Count);

        var results = new List<(string segmentId, float[] embedding)>();
        var batches = texts.Chunk(MAX_BATCH_SIZE);

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchTasks = batch.Select(async item =>
            {
                try
                {
                    var embedding = await GenerateEmbeddingAsync(item.text, cancellationToken);
                    return (item.segmentId, embedding);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for segment {SegmentId}", item.segmentId);
                    // Return null embedding for failed items
                    return (item.segmentId, Array.Empty<float>());
                }
            });

            var batchResults = await Task.WhenAll(batchTasks);
            results.AddRange(batchResults.Where(r => r.Item2.Length > 0));
        }

        _logger.LogInformation("Successfully generated {Count} embeddings", results.Count);
        return results;
    }

    /// <inheritdoc />
    public Task<int> GetEmbeddingDimensionAsync()
    {
        return Task.FromResult(EMBEDDING_DIMENSION);
    }

    /// <inheritdoc />
    public Task<bool> IsModelAvailableAsync()
    {
        // For MVP, always return true since we're using mock embeddings
        // In production, check if the actual model is loaded and ready
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public float CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        ArgumentNullException.ThrowIfNull(embedding1, nameof(embedding1));
        ArgumentNullException.ThrowIfNull(embedding2, nameof(embedding2));

        if (embedding1.Length != embedding2.Length)
        {
            throw new ArgumentException("Embeddings must have the same dimension");
        }

        if (embedding1.Length == 0)
        {
            return 0f;
        }

        // Calculate cosine similarity
        float dotProduct = 0f;
        float norm1 = 0f;
        float norm2 = 0f;

        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            norm1 += embedding1[i] * embedding1[i];
            norm2 += embedding2[i] * embedding2[i];
        }

        if (norm1 == 0f || norm2 == 0f)
        {
            return 0f;
        }

        return dotProduct / (MathF.Sqrt(norm1) * MathF.Sqrt(norm2));
    }

    /// <inheritdoc />
    public List<(string id, float similarity)> FindMostSimilar(
        float[] queryEmbedding,
        List<(string id, float[] embedding)> candidateEmbeddings,
        int topK = 10)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding, nameof(queryEmbedding));
        ArgumentNullException.ThrowIfNull(candidateEmbeddings, nameof(candidateEmbeddings));

        if (candidateEmbeddings.Count == 0)
        {
            return new List<(string, float)>();
        }

        var similarities = candidateEmbeddings
            .Select(candidate => new
            {
                candidate.id,
                similarity = CalculateSimilarity(queryEmbedding, candidate.embedding)
            })
            .OrderByDescending(x => x.similarity)
            .Take(topK)
            .Select(x => (x.id, x.similarity))
            .ToList();

        return similarities;
    }

    /// <summary>
    /// Generates a mock embedding for MVP purposes
    /// In production, replace with actual embedding model inference
    /// </summary>
    private float[] GenerateMockEmbedding(string text)
    {
        // Create deterministic embedding based on text content
        var embedding = new float[EMBEDDING_DIMENSION];

        // Use text hash for deterministic generation
        var hash = text.GetHashCode();
        var localRandom = new Random(hash);

        // Generate normalized random values
        float sum = 0f;
        for (int i = 0; i < EMBEDDING_DIMENSION; i++)
        {
            embedding[i] = (float)(localRandom.NextDouble() * 2 - 1); // Range [-1, 1]
            sum += embedding[i] * embedding[i];
        }

        // Normalize to unit vector (common practice for embeddings)
        if (sum > 0)
        {
            var norm = MathF.Sqrt(sum);
            for (int i = 0; i < EMBEDDING_DIMENSION; i++)
            {
                embedding[i] /= norm;
            }
        }

        return embedding;
    }

    /// <summary>
    /// Serializes an embedding vector to JSON string for storage
    /// </summary>
    public static string SerializeEmbedding(float[] embedding)
    {
        return JsonSerializer.Serialize(embedding);
    }

    /// <summary>
    /// Deserializes an embedding vector from JSON string
    /// </summary>
    public static float[]? DeserializeEmbedding(string? embeddingJson)
    {
        if (string.IsNullOrWhiteSpace(embeddingJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<float[]>(embeddingJson);
        }
        catch
        {
            return null;
        }
    }
}