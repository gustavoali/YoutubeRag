using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service implementation for intelligently segmenting text into chunks
/// </summary>
public partial class SegmentationService : ISegmentationService
{
    private readonly ILogger<SegmentationService> _logger;

    // Regex patterns for sentence and paragraph detection
    [GeneratedRegex(@"[.!?]+[\s\n]+", RegexOptions.Compiled)]
    private static partial Regex SentenceEndRegex();

    [GeneratedRegex(@"\n\n+", RegexOptions.Compiled)]
    private static partial Regex ParagraphSeparatorRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public SegmentationService(ILogger<SegmentationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<string>> SegmentTextAsync(string text, int maxSegmentLength = 500)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        if (maxSegmentLength <= 0)
        {
            throw new ArgumentException("Max segment length must be positive", nameof(maxSegmentLength));
        }

        _logger.LogDebug("Segmenting text of length {Length} with max segment length {MaxLength}",
            text.Length, maxSegmentLength);

        var segments = new List<string>();
        var sentences = SplitIntoSentences(text);
        var currentSegment = new StringBuilder();

        foreach (var sentence in sentences)
        {
            // If adding this sentence would exceed the max length, start a new segment
            if (currentSegment.Length > 0 &&
                currentSegment.Length + sentence.Length > maxSegmentLength)
            {
                var segmentText = currentSegment.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(segmentText))
                {
                    segments.Add(segmentText);
                }
                currentSegment.Clear();
            }

            currentSegment.Append(sentence);
            currentSegment.Append(' ');

            // If single sentence is too long, split it
            if (currentSegment.Length > maxSegmentLength)
            {
                var longSegment = currentSegment.ToString();
                currentSegment.Clear();

                var parts = SplitLongText(longSegment, maxSegmentLength);
                segments.AddRange(parts);
            }
        }

        // Add remaining segment
        if (currentSegment.Length > 0)
        {
            var segmentText = currentSegment.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(segmentText))
            {
                segments.Add(segmentText);
            }
        }

        _logger.LogDebug("Created {Count} segments from text", segments.Count);
        return Task.FromResult(segments);
    }

    /// <inheritdoc />
    public Task<List<TranscriptSegment>> MergeSmallSegmentsAsync(
        List<TranscriptSegment> segments,
        int minSegmentLength = 100)
    {
        ArgumentNullException.ThrowIfNull(segments, nameof(segments));

        if (segments.Count == 0)
        {
            return Task.FromResult(new List<TranscriptSegment>());
        }

        _logger.LogDebug("Merging {Count} segments with min length {MinLength}",
            segments.Count, minSegmentLength);

        var mergedSegments = new List<TranscriptSegment>();
        TranscriptSegment? currentMerged = null;

        foreach (var segment in segments.OrderBy(s => s.SegmentIndex))
        {
            if (currentMerged == null)
            {
                currentMerged = CloneSegment(segment);
                continue;
            }

            // Check if we should merge with current segment
            if (currentMerged.Text.Length < minSegmentLength)
            {
                // Merge segments
                currentMerged.Text = $"{currentMerged.Text} {segment.Text}";
                currentMerged.EndTime = segment.EndTime;

                // Update confidence as weighted average
                if (currentMerged.Confidence.HasValue && segment.Confidence.HasValue)
                {
                    var totalLength = currentMerged.Text.Length + segment.Text.Length;
                    currentMerged.Confidence = (currentMerged.Confidence.Value * currentMerged.Text.Length +
                                               segment.Confidence.Value * segment.Text.Length) / totalLength;
                }
            }
            else
            {
                // Current merged segment is large enough, save it
                mergedSegments.Add(currentMerged);
                currentMerged = CloneSegment(segment);
            }
        }

        // Add the last segment
        if (currentMerged != null)
        {
            mergedSegments.Add(currentMerged);
        }

        // Update segment indices
        for (int i = 0; i < mergedSegments.Count; i++)
        {
            mergedSegments[i].SegmentIndex = i;
        }

        _logger.LogDebug("Merged into {Count} segments", mergedSegments.Count);
        return Task.FromResult(mergedSegments);
    }

    /// <inheritdoc />
    public Task<List<TranscriptSegment>> CreateSegmentsFromTranscriptAsync(
        string videoId,
        string text,
        double startTime,
        double endTime,
        int maxSegmentLength = 500)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId, nameof(videoId));
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        _logger.LogDebug("Creating segments for video {VideoId} from {Start} to {End}",
            videoId, startTime, endTime);

        var textSegments = SegmentTextAsync(text, maxSegmentLength).Result;
        var segments = new List<TranscriptSegment>();

        var totalDuration = endTime - startTime;
        var totalTextLength = text.Length;

        double currentTime = startTime;

        for (int i = 0; i < textSegments.Count; i++)
        {
            var segmentText = textSegments[i];
            var segmentRatio = (double)segmentText.Length / totalTextLength;
            var segmentDuration = totalDuration * segmentRatio;

            var segment = new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                Text = segmentText,
                StartTime = currentTime,
                EndTime = currentTime + segmentDuration,
                SegmentIndex = i
                // CRITICAL FIX (ISSUE-002): Do NOT set CreatedAt/UpdatedAt here
                // Let the caller (TranscriptionJobProcessor) set these timestamps
                // with a shared timestamp for true bulk insert behavior
            };

            segments.Add(segment);
            currentTime = segment.EndTime;
        }

        // Ensure last segment ends at the correct time
        if (segments.Any())
        {
            segments[^1].EndTime = endTime;
        }

        _logger.LogDebug("Created {Count} transcript segments", segments.Count);
        return Task.FromResult(segments);
    }

    /// <inheritdoc />
    public async Task<List<string>> SegmentWithSemanticAwarenessAsync(
        string text,
        SegmentationOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        options ??= new SegmentationOptions();

        _logger.LogDebug("Performing semantic segmentation with options: MaxLength={MaxLength}, MinLength={MinLength}, Overlap={Overlap}",
            options.MaxSegmentLength, options.MinSegmentLength, options.OverlapLength);

        var segments = new List<string>();

        if (options.PreserveParagraphBoundaries)
        {
            var paragraphs = ParagraphSeparatorRegex().Split(text);
            foreach (var paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                    continue;

                var paragraphSegments = await SegmentParagraphAsync(paragraph, options);
                segments.AddRange(paragraphSegments);
            }
        }
        else
        {
            segments = await SegmentParagraphAsync(text, options);
        }

        // Apply overlap if specified
        if (options.OverlapLength > 0 && segments.Count > 1)
        {
            segments = ApplyOverlap(segments, options.OverlapLength);
        }

        // Limit number of segments if specified
        if (options.MaxSegments.HasValue && segments.Count > options.MaxSegments.Value)
        {
            segments = segments.Take(options.MaxSegments.Value).ToList();
        }

        _logger.LogDebug("Created {Count} semantic segments", segments.Count);
        return segments;
    }

    /// <summary>
    /// Segments a single paragraph respecting sentence boundaries
    /// </summary>
    private async Task<List<string>> SegmentParagraphAsync(string paragraph, SegmentationOptions options)
    {
        var segments = new List<string>();
        var sentences = options.PreserveSentenceBoundaries
            ? SplitIntoSentences(paragraph)
            : new List<string> { paragraph };

        var currentSegment = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSentence))
                continue;

            // Check if adding this sentence exceeds max length
            var potentialLength = currentSegment.Length + trimmedSentence.Length + (currentSegment.Length > 0 ? 1 : 0);

            if (potentialLength > options.MaxSegmentLength && currentSegment.Length > 0)
            {
                // Save current segment if it meets minimum length
                var segmentText = currentSegment.ToString().Trim();
                if (segmentText.Length >= options.MinSegmentLength)
                {
                    segments.Add(segmentText);
                    currentSegment.Clear();
                }
            }

            if (currentSegment.Length > 0)
            {
                currentSegment.Append(' ');
            }
            currentSegment.Append(trimmedSentence);

            // If single sentence is too long, handle it specially
            if (currentSegment.Length > options.MaxSegmentLength)
            {
                var longText = currentSegment.ToString();
                currentSegment.Clear();

                var parts = SplitLongText(longText, options.MaxSegmentLength);
                segments.AddRange(parts);
            }
        }

        // Add remaining segment
        if (currentSegment.Length > 0)
        {
            var segmentText = currentSegment.ToString().Trim();
            if (segmentText.Length >= options.MinSegmentLength || segments.Count == 0)
            {
                segments.Add(segmentText);
            }
            else if (segments.Count > 0)
            {
                // Merge with previous segment if too small
                segments[^1] = $"{segments[^1]} {segmentText}";
            }
        }

        return await Task.FromResult(segments);
    }

    /// <summary>
    /// Splits text into sentences
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var sentenceEnds = SentenceEndRegex().Matches(text);

        int lastIndex = 0;
        foreach (Match match in sentenceEnds)
        {
            var sentence = text.Substring(lastIndex, match.Index + match.Length - lastIndex);
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                sentences.Add(sentence.Trim());
            }
            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            var remaining = text.Substring(lastIndex).Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                sentences.Add(remaining);
            }
        }

        return sentences;
    }

    /// <summary>
    /// Splits long text that doesn't have natural boundaries.
    /// ENFORCES HARD LIMIT: No segment can exceed maxLength characters.
    /// If a single word exceeds maxLength, it will be forcibly split at character boundaries.
    /// </summary>
    private List<string> SplitLongText(string text, int maxLength)
    {
        var parts = new List<string>();
        var words = WhitespaceRegex().Split(text);
        var currentPart = new StringBuilder();

        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            // HARD LIMIT ENFORCEMENT: If adding this word would exceed maxLength, save current part
            if (currentPart.Length + word.Length + 1 > maxLength && currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString().Trim());
                currentPart.Clear();
            }

            // Handle extremely long words that exceed maxLength by themselves
            if (word.Length > maxLength)
            {
                _logger.LogWarning("Encountered word longer than maxLength ({Length} > {MaxLength}). Forcibly splitting at character boundaries.",
                    word.Length, maxLength);

                // If current part has content, save it first
                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString().Trim());
                    currentPart.Clear();
                }

                // Split the long word at character boundaries
                var remainingWord = word;
                while (remainingWord.Length > maxLength)
                {
                    parts.Add(remainingWord.Substring(0, maxLength));
                    remainingWord = remainingWord.Substring(maxLength);
                }

                // Add remaining portion if any
                if (remainingWord.Length > 0)
                {
                    currentPart.Append(remainingWord);
                }

                continue;
            }

            // Normal case: add word to current part
            if (currentPart.Length > 0)
            {
                currentPart.Append(' ');
            }
            currentPart.Append(word);

            // SAFETY CHECK: Even after adding a normal word, verify we haven't exceeded maxLength
            // This handles edge cases where the StringBuilder might exceed limit
            if (currentPart.Length > maxLength)
            {
                _logger.LogWarning("Current part exceeded maxLength after adding word. Length: {Length}, MaxLength: {MaxLength}. Splitting.",
                    currentPart.Length, maxLength);

                var oversizedText = currentPart.ToString();
                currentPart.Clear();

                // Recursively split the oversized text to ensure compliance
                var splitParts = SplitAtCharacterBoundary(oversizedText, maxLength);
                parts.AddRange(splitParts);
            }
        }

        // Add final part if any
        if (currentPart.Length > 0)
        {
            var finalText = currentPart.ToString().Trim();

            // Final safety check: ensure last part doesn't exceed maxLength
            if (finalText.Length > maxLength)
            {
                _logger.LogWarning("Final part exceeds maxLength ({Length} > {MaxLength}). Splitting.",
                    finalText.Length, maxLength);

                var splitParts = SplitAtCharacterBoundary(finalText, maxLength);
                parts.AddRange(splitParts);
            }
            else if (finalText.Length > 0)
            {
                parts.Add(finalText);
            }
        }

        // POST-PROCESSING VALIDATION: Verify no part exceeds maxLength
        foreach (var part in parts)
        {
            if (part.Length > maxLength)
            {
                _logger.LogError("CRITICAL: SplitLongText produced part with length {Length} exceeding maxLength {MaxLength}. This should never happen.",
                    part.Length, maxLength);
            }
        }

        return parts;
    }

    /// <summary>
    /// Splits text at character boundaries when word-based splitting is not possible.
    /// Used as a fallback for extremely long text without whitespace boundaries.
    /// </summary>
    private List<string> SplitAtCharacterBoundary(string text, int maxLength)
    {
        var parts = new List<string>();

        for (int i = 0; i < text.Length; i += maxLength)
        {
            var remainingLength = text.Length - i;
            var chunkLength = Math.Min(maxLength, remainingLength);
            parts.Add(text.Substring(i, chunkLength));
        }

        return parts;
    }

    /// <summary>
    /// Applies overlap between consecutive segments
    /// </summary>
    private List<string> ApplyOverlap(List<string> segments, int overlapLength)
    {
        if (segments.Count <= 1 || overlapLength <= 0)
        {
            return segments;
        }

        var overlappedSegments = new List<string>();

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // Add overlap from previous segment
            if (i > 0)
            {
                var prevSegment = segments[i - 1];
                var overlapText = GetLastNCharacters(prevSegment, overlapLength);
                if (!string.IsNullOrWhiteSpace(overlapText))
                {
                    segment = $"{overlapText} {segment}";
                }
            }

            // Add overlap from next segment
            if (i < segments.Count - 1)
            {
                var nextSegment = segments[i + 1];
                var overlapText = GetFirstNCharacters(nextSegment, overlapLength);
                if (!string.IsNullOrWhiteSpace(overlapText))
                {
                    segment = $"{segment} {overlapText}";
                }
            }

            overlappedSegments.Add(segment);
        }

        return overlappedSegments;
    }

    /// <summary>
    /// Gets the first N characters from text
    /// </summary>
    private string GetFirstNCharacters(string text, int n)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= n ? text : text.Substring(0, n);
    }

    /// <summary>
    /// Gets the last N characters from text
    /// </summary>
    private string GetLastNCharacters(string text, int n)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= n ? text : text.Substring(text.Length - n);
    }

    /// <summary>
    /// Creates a deep copy of a transcript segment
    /// </summary>
    private TranscriptSegment CloneSegment(TranscriptSegment segment)
    {
        return new TranscriptSegment
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = segment.VideoId,
            Text = segment.Text,
            StartTime = segment.StartTime,
            EndTime = segment.EndTime,
            SegmentIndex = segment.SegmentIndex,
            Confidence = segment.Confidence,
            Language = segment.Language,
            Speaker = segment.Speaker,
            CreatedAt = segment.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }
}