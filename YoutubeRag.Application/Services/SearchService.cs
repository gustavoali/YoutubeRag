using AutoMapper;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.DTOs.Search;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Service implementation for semantic search operations
/// </summary>
public class SearchService : ISearchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchService> _logger;
    private readonly IEmbeddingService _embeddingService;

    public SearchService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SearchService> logger,
        IEmbeddingService embeddingService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _embeddingService = embeddingService;
    }

    public async Task<SearchResponseDto> SearchAsync(SearchRequestDto searchDto)
    {
        _logger.LogInformation("Performing search: {Query}", searchDto.Query);

        // Generate embedding for search query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchDto.Query);

        // Get all transcript segments
        var allSegments = await _unitOfWork.TranscriptSegments.GetAllAsync();

        // Calculate similarity scores
        var results = new List<SearchResultDto>();

        foreach (var segment in allSegments)
        {
            if (string.IsNullOrEmpty(segment.EmbeddingVector))
            {
                continue;
            }

            var segmentEmbedding = DeserializeEmbedding(segment.EmbeddingVector);
            if (segmentEmbedding == null)
            {
                continue;
            }

            var similarity = CalculateCosineSimilarity(queryEmbedding, segmentEmbedding);

            if (similarity >= (searchDto.MinScore ?? 0.0))
            {
                var video = await _unitOfWork.Videos.GetByIdAsync(segment.VideoId);
                if (video == null)
                {
                    continue;
                }

                results.Add(new SearchResultDto(
                    VideoId: video.Id,
                    VideoTitle: video.Title,
                    SegmentId: segment.Id,
                    SegmentText: segment.Text,
                    StartTime: segment.StartTime,
                    EndTime: segment.EndTime,
                    Score: similarity,
                    Timestamp: segment.StartTime
                ));
            }
        }

        // Sort by score descending
        results = results.OrderByDescending(r => r.Score).ToList();

        // Apply limit and offset
        var offset = searchDto.Offset ?? 0;
        var limit = searchDto.Limit ?? 10;

        var paginatedResults = results.Skip(offset).Take(limit).ToList();

        _logger.LogInformation("Search completed: {Query} - Found {Count} results", searchDto.Query, results.Count);

        return new SearchResponseDto(
            Query: searchDto.Query,
            Results: paginatedResults,
            TotalResults: results.Count,
            Limit: limit,
            Offset: offset
        );
    }

    public async Task<SearchResponseDto> SearchByVideoAsync(string videoId, SearchRequestDto searchDto)
    {
        _logger.LogInformation("Performing search in video {VideoId}: {Query}", videoId, searchDto.Query);

        // Verify video exists
        var video = await _unitOfWork.Videos.GetByIdAsync(videoId);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", videoId);
        }

        // Generate embedding for search query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchDto.Query);

        // Get transcript segments for this video
        var segments = await _unitOfWork.TranscriptSegments.FindAsync(s => s.VideoId == videoId);

        // Calculate similarity scores
        var results = new List<SearchResultDto>();

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment.EmbeddingVector))
            {
                continue;
            }

            var segmentEmbedding = DeserializeEmbedding(segment.EmbeddingVector);
            if (segmentEmbedding == null)
            {
                continue;
            }

            var similarity = CalculateCosineSimilarity(queryEmbedding, segmentEmbedding);

            if (similarity >= (searchDto.MinScore ?? 0.0))
            {
                results.Add(new SearchResultDto(
                    VideoId: video.Id,
                    VideoTitle: video.Title,
                    SegmentId: segment.Id,
                    SegmentText: segment.Text,
                    StartTime: segment.StartTime,
                    EndTime: segment.EndTime,
                    Score: similarity,
                    Timestamp: segment.StartTime
                ));
            }
        }

        // Sort by score descending
        results = results.OrderByDescending(r => r.Score).ToList();

        // Apply limit and offset
        var offset = searchDto.Offset ?? 0;
        var limit = searchDto.Limit ?? 10;

        var paginatedResults = results.Skip(offset).Take(limit).ToList();

        _logger.LogInformation("Video search completed: {VideoId} - Found {Count} results", videoId, results.Count);

        return new SearchResponseDto(
            Query: searchDto.Query,
            Results: paginatedResults,
            TotalResults: results.Count,
            Limit: limit,
            Offset: offset
        );
    }

    public async Task<List<SearchResultDto>> GetSimilarVideosAsync(string videoId, int limit = 10)
    {
        _logger.LogInformation("Finding similar videos for: {VideoId}", videoId);

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", videoId);
        }

        // Get transcript segments for this video
        var sourceSegments = await _unitOfWork.TranscriptSegments.FindAsync(s => s.VideoId == videoId);

        if (!sourceSegments.Any())
        {
            _logger.LogWarning("No transcript segments found for video: {VideoId}", videoId);
            return new List<SearchResultDto>();
        }

        // Calculate average embedding for the video
        var embeddings = sourceSegments
            .Where(s => !string.IsNullOrEmpty(s.EmbeddingVector))
            .Select(s => DeserializeEmbedding(s.EmbeddingVector))
            .Where(e => e != null)
            .Cast<double[]>()
            .ToList();

        var avgEmbedding = CalculateAverageEmbedding(embeddings);

        if (avgEmbedding == null)
        {
            return new List<SearchResultDto>();
        }

        // Get all other videos
        var allVideos = await _unitOfWork.Videos.FindAsync(v => v.Id != videoId);
        var similarVideos = new List<(string VideoId, double Score)>();

        foreach (var otherVideo in allVideos)
        {
            var otherSegments = await _unitOfWork.TranscriptSegments.FindAsync(s => s.VideoId == otherVideo.Id);
            var otherEmbeddings = otherSegments
                .Where(s => !string.IsNullOrEmpty(s.EmbeddingVector))
                .Select(s => DeserializeEmbedding(s.EmbeddingVector))
                .Where(e => e != null)
                .Cast<double[]>()
                .ToList();

            if (!otherEmbeddings.Any())
            {
                continue;
            }

            var otherAvgEmbedding = CalculateAverageEmbedding(otherEmbeddings);
            if (otherAvgEmbedding == null)
            {
                continue;
            }

            var similarity = CalculateCosineSimilarity(avgEmbedding, otherAvgEmbedding);
            similarVideos.Add((otherVideo.Id, similarity));
        }

        // Sort by similarity and take top N
        var results = similarVideos
            .OrderByDescending(v => v.Score)
            .Take(limit)
            .Select(v =>
            {
                var vid = allVideos.First(x => x.Id == v.VideoId);
                return new SearchResultDto(
                    VideoId: vid.Id,
                    VideoTitle: vid.Title,
                    SegmentId: string.Empty,
                    SegmentText: vid.Description ?? string.Empty,
                    StartTime: 0,
                    EndTime: 0,
                    Score: v.Score,
                    Timestamp: 0
                );
            })
            .ToList();

        _logger.LogInformation("Found {Count} similar videos for: {VideoId}", results.Count, videoId);

        return results;
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string partialQuery, int limit = 5)
    {
        _logger.LogInformation("Getting search suggestions for: {Query}", partialQuery);

        // Get recent searches or popular queries
        // This is a simple implementation - in production you would track search history

        var segments = await _unitOfWork.TranscriptSegments.GetAllAsync();

        var suggestions = segments
            .Where(s => s.Text.Contains(partialQuery, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Text)
            .Distinct()
            .Take(limit)
            .ToList();

        return suggestions;
    }

    #region Private Helper Methods

    private double CalculateCosineSimilarity(float[] vectorA, double[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private double CalculateCosineSimilarity(double[] vectorA, double[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private double[]? CalculateAverageEmbedding(List<double[]> embeddings)
    {
        if (!embeddings.Any())
        {
            return null;
        }

        var dimension = embeddings[0].Length;
        var avgEmbedding = new double[dimension];

        foreach (var embedding in embeddings)
        {
            for (int i = 0; i < dimension; i++)
            {
                avgEmbedding[i] += embedding[i];
            }
        }

        for (int i = 0; i < dimension; i++)
        {
            avgEmbedding[i] /= embeddings.Count;
        }

        return avgEmbedding;
    }

    private double[]? DeserializeEmbedding(string embeddingVector)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<double[]>(embeddingVector);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
