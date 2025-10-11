namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service for generating text embeddings for semantic search and similarity matching
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for a single text
    /// </summary>
    /// <param name="text">The text to generate embedding for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The embedding vector as a float array</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts in batch for efficiency
    /// </summary>
    /// <param name="texts">List of tuples containing segment IDs and their corresponding texts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tuples containing segment IDs and their embedding vectors</returns>
    Task<List<(string segmentId, float[] embedding)>> GenerateEmbeddingsAsync(
        List<(string segmentId, string text)> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dimension size of the embedding vectors
    /// </summary>
    /// <returns>The dimension of embedding vectors (e.g., 384, 768, 1536)</returns>
    Task<int> GetEmbeddingDimensionAsync();

    /// <summary>
    /// Checks if the embedding model is available and ready
    /// </summary>
    /// <returns>True if the model is available, false otherwise</returns>
    Task<bool> IsModelAvailableAsync();

    /// <summary>
    /// Calculates similarity between two embedding vectors
    /// </summary>
    /// <param name="embedding1">First embedding vector</param>
    /// <param name="embedding2">Second embedding vector</param>
    /// <returns>Similarity score between 0 and 1</returns>
    float CalculateSimilarity(float[] embedding1, float[] embedding2);

    /// <summary>
    /// Finds the most similar embeddings from a list
    /// </summary>
    /// <param name="queryEmbedding">The query embedding to compare against</param>
    /// <param name="candidateEmbeddings">List of candidate embeddings with their IDs</param>
    /// <param name="topK">Number of top results to return</param>
    /// <returns>Top K most similar embeddings with their similarity scores</returns>
    List<(string id, float similarity)> FindMostSimilar(
        float[] queryEmbedding,
        List<(string id, float[] embedding)> candidateEmbeddings,
        int topK = 10);
}
