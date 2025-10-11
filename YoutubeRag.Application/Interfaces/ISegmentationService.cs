using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service for intelligently segmenting text into chunks suitable for embedding
/// </summary>
public interface ISegmentationService
{
    /// <summary>
    /// Segments text into chunks optimized for embedding generation
    /// </summary>
    /// <param name="text">The text to segment</param>
    /// <param name="maxSegmentLength">Maximum length of each segment in characters</param>
    /// <returns>List of text segments</returns>
    Task<List<string>> SegmentTextAsync(string text, int maxSegmentLength = 500);

    /// <summary>
    /// Merges small segments to reach minimum size thresholds
    /// </summary>
    /// <param name="segments">List of transcript segments to merge</param>
    /// <param name="minSegmentLength">Minimum segment length in characters</param>
    /// <returns>List of merged segments</returns>
    Task<List<TranscriptSegment>> MergeSmallSegmentsAsync(
        List<TranscriptSegment> segments,
        int minSegmentLength = 100);

    /// <summary>
    /// Creates transcript segments from raw transcript text with timestamps
    /// </summary>
    /// <param name="videoId">The video ID these segments belong to</param>
    /// <param name="text">The full transcript text</param>
    /// <param name="startTime">Start time for the transcript</param>
    /// <param name="endTime">End time for the transcript</param>
    /// <param name="maxSegmentLength">Maximum segment length</param>
    /// <returns>List of created transcript segments</returns>
    Task<List<TranscriptSegment>> CreateSegmentsFromTranscriptAsync(
        string videoId,
        string text,
        double startTime,
        double endTime,
        int maxSegmentLength = 500);

    /// <summary>
    /// Segments text with semantic awareness, preserving sentence boundaries
    /// </summary>
    /// <param name="text">The text to segment</param>
    /// <param name="options">Segmentation options</param>
    /// <returns>List of semantically coherent segments</returns>
    Task<List<string>> SegmentWithSemanticAwarenessAsync(
        string text,
        SegmentationOptions? options = null);
}

/// <summary>
/// Options for controlling text segmentation behavior
/// </summary>
public class SegmentationOptions
{
    /// <summary>
    /// Maximum length of each segment in characters
    /// </summary>
    public int MaxSegmentLength { get; set; } = 500;

    /// <summary>
    /// Minimum length of each segment in characters
    /// </summary>
    public int MinSegmentLength { get; set; } = 100;

    /// <summary>
    /// Overlap between consecutive segments in characters
    /// </summary>
    public int OverlapLength { get; set; } = 50;

    /// <summary>
    /// Whether to preserve sentence boundaries
    /// </summary>
    public bool PreserveSentenceBoundaries { get; set; } = true;

    /// <summary>
    /// Whether to preserve paragraph boundaries
    /// </summary>
    public bool PreserveParagraphBoundaries { get; set; } = false;

    /// <summary>
    /// Maximum number of segments to generate
    /// </summary>
    public int? MaxSegments { get; set; }

    /// <summary>
    /// Language code for language-specific segmentation rules
    /// </summary>
    public string? LanguageCode { get; set; }
}
