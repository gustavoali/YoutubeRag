namespace YoutubeRag.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
    Task<List<SearchResult>> SearchSimilarAsync(string query, int limit = 10, double threshold = 0.7);
    Task<bool> IndexTranscriptSegmentsAsync(string videoId, List<string> segments);
    Task<bool> DeleteVideoEmbeddingsAsync(string videoId);
}

public class SearchResult
{
    public string VideoId { get; set; } = string.Empty;
    public string SegmentId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double Similarity { get; set; }
    public string VideoTitle { get; set; } = string.Empty;
    public string VideoThumbnail { get; set; } = string.Empty;
}