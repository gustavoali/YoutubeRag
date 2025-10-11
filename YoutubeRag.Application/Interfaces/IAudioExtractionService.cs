namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Service interface for extracting audio from video sources
/// </summary>
public interface IAudioExtractionService
{
    /// <summary>
    /// Extracts audio from a YouTube video
    /// </summary>
    /// <param name="youTubeId">YouTube video ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the extracted audio file</returns>
    Task<string> ExtractAudioFromYouTubeAsync(string youTubeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts audio from a local video file
    /// </summary>
    /// <param name="videoFilePath">Path to the video file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the extracted audio file</returns>
    Task<string> ExtractAudioFromVideoFileAsync(string videoFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts Whisper-compatible audio (16kHz mono WAV) from a video file
    /// </summary>
    /// <param name="videoFilePath">Path to the video file</param>
    /// <param name="videoId">Video ID for naming the audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the extracted Whisper-compatible audio file</returns>
    Task<string> ExtractWhisperAudioFromVideoAsync(string videoFilePath, string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an audio file from storage
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteAudioFileAsync(string audioFilePath);

    /// <summary>
    /// Gets detailed information about an audio file
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio information including duration, format, and size</returns>
    Task<AudioInfo> GetAudioInfoAsync(string audioFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if an audio file exists and is accessible
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <returns>True if the file exists and is accessible</returns>
    Task<bool> ValidateAudioFileAsync(string audioFilePath);

    /// <summary>
    /// Gets the configured audio storage directory path
    /// </summary>
    /// <returns>Path to the audio storage directory</returns>
    string GetAudioStoragePath();
}

/// <summary>
/// Contains detailed information about an audio file
/// </summary>
public class AudioInfo
{
    /// <summary>
    /// Duration of the audio
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Audio format (e.g., mp3, wav, m4a)
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Sample rate in Hz (e.g., 44100, 48000)
    /// </summary>
    public int SampleRate { get; set; }

    /// <summary>
    /// Number of audio channels (1 = mono, 2 = stereo)
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Bitrate in bits per second
    /// </summary>
    public int Bitrate { get; set; }

    /// <summary>
    /// File size in a human-readable format
    /// </summary>
    public string FormattedFileSize => FormatFileSize(FileSizeBytes);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
