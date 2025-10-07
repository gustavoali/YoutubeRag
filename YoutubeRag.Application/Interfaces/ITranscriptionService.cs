using YoutubeRag.Application.DTOs.Transcription;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAudioAsync(string audioFilePath, string language = "auto");
    Task<TranscriptionResult> TranscribeAudioFromUrlAsync(string audioUrl, string language = "auto");
    Task<List<TranscriptSegment>> ProcessTranscriptionAsync(string videoId, TranscriptionResult transcription);
    Task<bool> IsLanguageSupportedAsync(string language);
    Task<List<string>> GetSupportedLanguagesAsync();

    /// <summary>
    /// Transcribes audio using the provided request parameters
    /// </summary>
    /// <param name="request">Transcription request with audio file path and settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcription result with segments and metadata</returns>
    Task<TranscriptionResultDto> TranscribeAudioAsync(TranscriptionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Whisper is available and properly configured
    /// </summary>
    /// <returns>True if Whisper is available and ready to use</returns>
    Task<bool> IsWhisperAvailableAsync();

    /// <summary>
    /// Gets the version of the installed Whisper
    /// </summary>
    /// <returns>Whisper version string or null if not available</returns>
    Task<string> GetWhisperVersionAsync();
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