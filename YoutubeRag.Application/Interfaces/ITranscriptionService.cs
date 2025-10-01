using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAudioAsync(string audioFilePath, string language = "auto");
    Task<TranscriptionResult> TranscribeAudioFromUrlAsync(string audioUrl, string language = "auto");
    Task<List<TranscriptSegment>> ProcessTranscriptionAsync(string videoId, TranscriptionResult transcription);
    Task<bool> IsLanguageSupportedAsync(string language);
    Task<List<string>> GetSupportedLanguagesAsync();
}

public class TranscriptionResult
{
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public TimeSpan Duration { get; set; }
    public List<TranscriptionSegment> Segments { get; set; } = new();
}

public class TranscriptionSegment
{
    public int Index { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<Word> Words { get; set; } = new();
}

public class Word
{
    public string Text { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double Confidence { get; set; }
}