using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Infrastructure.Services;

namespace YoutubeRag.Tests.Integration.Services;

/// <summary>
/// Unit tests for LocalEmbeddingService (GAP 5: Mock Embeddings)
/// Tests mock embedding generation for transcript segments
/// Validates deterministic behavior, normalization, and similarity calculations
/// </summary>
public class LocalEmbeddingServiceTests
{
    private IEmbeddingService CreateService()
    {
        var mockLogger = new Mock<ILogger<LocalEmbeddingService>>();
        return new LocalEmbeddingService(mockLogger.Object);
    }

    #region Basic Embedding Generation Tests

    [Fact]
    public async Task GenerateEmbeddingAsync_WithValidText_ReturnsVectorOf384Dimensions()
    {
        // Arrange
        var service = CreateService();
        var text = "This is a test transcript segment for embedding generation.";

        // Act
        var embedding = await service.GenerateEmbeddingAsync(text);

        // Assert
        embedding.Should().NotBeNull();
        embedding.Should().HaveCount(384, "standard sentence-transformers dimension");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithValidText_ReturnsNormalizedVector()
    {
        // Arrange
        var service = CreateService();
        var text = "Test segment for normalization verification.";

        // Act
        var embedding = await service.GenerateEmbeddingAsync(text);

        // Assert - Calculate L2 norm (should be approximately 1.0 for normalized vectors)
        var norm = CalculateL2Norm(embedding);
        norm.Should().BeApproximately(1.0f, 0.0001f, "embedding should be normalized to unit length");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithSameText_ReturnsDeterministicResults()
    {
        // Arrange
        var service = CreateService();
        var text = "Deterministic test segment.";

        // Act
        var embedding1 = await service.GenerateEmbeddingAsync(text);
        var embedding2 = await service.GenerateEmbeddingAsync(text);

        // Assert
        embedding1.Should().BeEquivalentTo(embedding2,
            "same text should always produce identical embeddings");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithDifferentText_ReturnsDifferentEmbeddings()
    {
        // Arrange
        var service = CreateService();
        var text1 = "First test segment.";
        var text2 = "Second test segment.";

        // Act
        var embedding1 = await service.GenerateEmbeddingAsync(text1);
        var embedding2 = await service.GenerateEmbeddingAsync(text2);

        // Assert
        embedding1.Should().NotBeEquivalentTo(embedding2,
            "different texts should produce different embeddings");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.GenerateEmbeddingAsync(""));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.GenerateEmbeddingAsync("   "));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNullText_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.GenerateEmbeddingAsync(null!));
    }

    #endregion

    #region Batch Embedding Generation Tests

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMultipleTexts_ReturnsAllEmbeddings()
    {
        // Arrange
        var service = CreateService();
        var texts = new List<(string segmentId, string text)>
        {
            ("seg-001", "First transcript segment."),
            ("seg-002", "Second transcript segment."),
            ("seg-003", "Third transcript segment.")
        };

        // Act
        var results = await service.GenerateEmbeddingsAsync(texts);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.embedding.Should().HaveCount(384));

        results[0].segmentId.Should().Be("seg-001");
        results[1].segmentId.Should().Be("seg-002");
        results[2].segmentId.Should().Be("seg-003");
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var texts = new List<(string segmentId, string text)>();

        // Act
        var results = await service.GenerateEmbeddingsAsync(texts);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithNullList_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.GenerateEmbeddingsAsync(null!));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_ProcessesInBatches()
    {
        // Arrange
        var service = CreateService();
        // Create 40 segments to test batching (MAX_BATCH_SIZE = 32)
        var texts = Enumerable.Range(1, 40)
            .Select(i => ($"seg-{i:000}", $"Transcript segment number {i}."))
            .ToList();

        // Act
        var results = await service.GenerateEmbeddingsAsync(texts);

        // Assert
        results.Should().HaveCount(40, "all segments should be processed");
        results.Should().AllSatisfy(r => r.embedding.Should().HaveCount(384));
    }

    #endregion

    #region Similarity Calculation Tests

    [Fact]
    public void CalculateSimilarity_WithIdenticalVectors_ReturnsOne()
    {
        // Arrange
        var service = CreateService();
        var vector = GenerateTestVector(384);

        // Act
        var similarity = service.CalculateSimilarity(vector, vector);

        // Assert
        similarity.Should().BeApproximately(1.0f, 0.0001f,
            "identical vectors should have similarity of 1.0");
    }

    [Fact]
    public void CalculateSimilarity_WithOrthogonalVectors_ReturnsZero()
    {
        // Arrange
        var service = CreateService();
        var vector1 = new float[384];
        var vector2 = new float[384];

        // Create orthogonal vectors
        vector1[0] = 1.0f;
        vector2[1] = 1.0f;

        // Act
        var similarity = service.CalculateSimilarity(vector1, vector2);

        // Assert
        similarity.Should().BeApproximately(0.0f, 0.0001f,
            "orthogonal vectors should have similarity of 0.0");
    }

    [Fact]
    public void CalculateSimilarity_WithDifferentDimensions_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var vector1 = new float[384];
        var vector2 = new float[256];

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            service.CalculateSimilarity(vector1, vector2));
    }

    [Fact]
    public void CalculateSimilarity_WithNullVectors_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var vector = new float[384];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.CalculateSimilarity(null!, vector));

        Assert.Throws<ArgumentNullException>(() =>
            service.CalculateSimilarity(vector, null!));
    }

    [Fact]
    public void CalculateSimilarity_WithEmptyVectors_ReturnsZero()
    {
        // Arrange
        var service = CreateService();
        var vector1 = Array.Empty<float>();
        var vector2 = Array.Empty<float>();

        // Act
        var similarity = service.CalculateSimilarity(vector1, vector2);

        // Assert
        similarity.Should().Be(0.0f);
    }

    #endregion

    #region FindMostSimilar Tests

    [Fact]
    public void FindMostSimilar_WithMultipleCandidates_ReturnsTopK()
    {
        // Arrange
        var service = CreateService();
        var queryEmbedding = GenerateTestVector(384, seed: 1);

        var candidates = new List<(string id, float[] embedding)>
        {
            ("candidate1", GenerateTestVector(384, seed: 1)),   // Identical
            ("candidate2", GenerateTestVector(384, seed: 2)),
            ("candidate3", GenerateTestVector(384, seed: 3)),
            ("candidate4", GenerateTestVector(384, seed: 4)),
            ("candidate5", GenerateTestVector(384, seed: 5))
        };

        // Act
        var results = service.FindMostSimilar(queryEmbedding, candidates, topK: 3);

        // Assert
        results.Should().HaveCount(3);
        results[0].id.Should().Be("candidate1", "identical vector should be most similar");
        results[0].similarity.Should().BeApproximately(1.0f, 0.0001f);

        // Results should be ordered by similarity descending
        results[0].similarity.Should().BeGreaterThanOrEqualTo(results[1].similarity);
        results[1].similarity.Should().BeGreaterThanOrEqualTo(results[2].similarity);
    }

    [Fact]
    public void FindMostSimilar_WithEmptyCandidates_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();
        var queryEmbedding = GenerateTestVector(384);
        var candidates = new List<(string id, float[] embedding)>();

        // Act
        var results = service.FindMostSimilar(queryEmbedding, candidates);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void FindMostSimilar_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var candidates = new List<(string id, float[] embedding)>
        {
            ("candidate1", GenerateTestVector(384))
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.FindMostSimilar(null!, candidates));
    }

    [Fact]
    public void FindMostSimilar_WithNullCandidates_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var queryEmbedding = GenerateTestVector(384);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            service.FindMostSimilar(queryEmbedding, null!));
    }

    #endregion

    #region Model Availability Tests

    [Fact]
    public async Task IsModelAvailableAsync_AlwaysReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var isAvailable = await service.IsModelAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue("mock embedding service should always be available");
    }

    [Fact]
    public async Task GetEmbeddingDimensionAsync_Returns384()
    {
        // Arrange
        var service = CreateService();

        // Act
        var dimension = await service.GetEmbeddingDimensionAsync();

        // Assert
        dimension.Should().Be(384, "standard sentence-transformers dimension");
    }

    #endregion

    #region Helper Methods

    private static float CalculateL2Norm(float[] vector)
    {
        var sum = 0.0f;
        foreach (var value in vector)
        {
            sum += value * value;
        }

        return MathF.Sqrt(sum);
    }

    private static float[] GenerateTestVector(int dimension, int seed = 42)
    {
        var random = new Random(seed);
        var vector = new float[dimension];

        var sum = 0.0f;
        for (int i = 0; i < dimension; i++)
        {
            vector[i] = (float)(random.NextDouble() * 2 - 1);
            sum += vector[i] * vector[i];
        }

        // Normalize
        if (sum > 0)
        {
            var norm = MathF.Sqrt(sum);
            for (int i = 0; i < dimension; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }

    #endregion
}
