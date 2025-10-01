using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace YoutubeRag.Infrastructure.Services;

public class LocalWhisperService : ITranscriptionService
{
    private readonly ILogger<LocalWhisperService> _logger;
    private readonly string _whisperPath;

    public LocalWhisperService(ILogger<LocalWhisperService> logger)
    {
        _logger = logger;
        _whisperPath = string.Empty; // Don't cache the path, find it dynamically
    }

    public async Task<TranscriptionResult> TranscribeAudioAsync(string audioFilePath, string language = "auto")
    {
        try
        {
            _logger.LogInformation("Local Whisper: Transcribing audio file: {FilePath}", audioFilePath);

            // Find Whisper executable dynamically each time
            var whisperPath = FindWhisperExecutable();
            if (string.IsNullOrEmpty(whisperPath))
            {
                throw new InvalidOperationException("Whisper executable not found. Please install whisper: pip install openai-whisper");
            }

            var outputFile = Path.ChangeExtension(audioFilePath, ".json");
            var languageParam = language == "auto" ? "" : $"--language {language}";

            var arguments = $"\"{audioFilePath}\" --model base --output_format json --output_dir \"{Path.GetDirectoryName(outputFile)}\" --verbose False {languageParam}";

            var processInfo = new ProcessStartInfo
            {
                FileName = whisperPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set environment variables to handle Unicode encoding properly
            processInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            processInfo.Environment["PYTHONUTF8"] = "1";
            processInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            processInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start Whisper process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Whisper failed: {error}");
            }

            // Parse JSON output
            var result = await ParseWhisperOutput(outputFile);

            // Clean up temporary file
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            _logger.LogInformation("Local Whisper: Successfully transcribed audio, duration: {Duration}, segments: {Count}",
                result.Duration, result.Segments.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local Whisper: Error transcribing audio file: {FilePath}", audioFilePath);
            throw;
        }
    }

    public async Task<TranscriptionResult> TranscribeAudioFromUrlAsync(string audioUrl, string language = "auto")
    {
        var tempFileName = $"temp_audio_{Guid.NewGuid()}.mp3";
        var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

        try
        {
            using var httpClient = new HttpClient();
            var audioData = await httpClient.GetByteArrayAsync(audioUrl);
            await File.WriteAllBytesAsync(tempFilePath, audioData);

            return await TranscribeAudioAsync(tempFilePath, language);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    public async Task<List<TranscriptSegment>> ProcessTranscriptionAsync(string videoId, TranscriptionResult transcription)
    {
        var segments = new List<TranscriptSegment>();

        for (int i = 0; i < transcription.Segments.Count; i++)
        {
            var segment = transcription.Segments[i];

            var transcriptSegment = new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = i,
                StartTime = segment.StartTime,
                EndTime = segment.EndTime,
                Text = segment.Text.Trim(),
                Language = transcription.Language,
                Confidence = segment.Confidence,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            segments.Add(transcriptSegment);
        }

        return segments;
    }

    public async Task<bool> IsLanguageSupportedAsync(string language)
    {
        var supportedLanguages = await GetSupportedLanguagesAsync();
        return supportedLanguages.Contains(language.ToLowerInvariant());
    }

    public async Task<List<string>> GetSupportedLanguagesAsync()
    {
        return await Task.FromResult(new List<string>
        {
            "auto", "en", "es", "fr", "de", "it", "pt", "ru", "ja", "ko", "zh", "ar", "hi", "tr", "pl", "nl", "sv", "da", "no", "fi"
        });
    }

    private string FindWhisperExecutable()
    {
        var possiblePaths = new[]
        {
            "whisper",
            "/usr/local/bin/whisper",
            "/opt/homebrew/bin/whisper",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "whisper"),
            // Windows Store Python paths
            @"C:\Users\gdali\AppData\Local\Packages\PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0\LocalCache\local-packages\Python313\Scripts\whisper.exe"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Set environment variables for Unicode support
                processInfo.Environment["PYTHONIOENCODING"] = "utf-8";
                processInfo.Environment["PYTHONUTF8"] = "1";
                processInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                processInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation("Found Whisper executable at: {Path}", path);
                        return path;
                    }
                }
            }
            catch
            {
                // Continue searching
            }
        }

        _logger.LogWarning("Whisper executable not found. Install with: pip install openai-whisper");
        return string.Empty;
    }

    private async Task<TranscriptionResult> ParseWhisperOutput(string outputFile)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(outputFile);
            var whisperResult = System.Text.Json.JsonSerializer.Deserialize<WhisperOutput>(jsonContent);

            var result = new TranscriptionResult
            {
                Text = whisperResult?.text ?? "",
                Language = whisperResult?.language ?? "unknown",
                Duration = TimeSpan.FromSeconds(whisperResult?.segments?.LastOrDefault()?.end ?? 0),
                Segments = new List<TranscriptionSegment>()
            };

            if (whisperResult?.segments != null)
            {
                for (int i = 0; i < whisperResult.segments.Length; i++)
                {
                    var segment = whisperResult.segments[i];
                    result.Segments.Add(new TranscriptionSegment
                    {
                        Index = i,
                        StartTime = segment.start,
                        EndTime = segment.end,
                        Text = segment.text?.Trim() ?? "",
                        Confidence = 0.9, // Whisper doesn't provide confidence scores
                        Words = new List<Word>()
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Whisper output file: {FilePath}", outputFile);
            throw;
        }
    }

    private class WhisperOutput
    {
        public string? text { get; set; }
        public string? language { get; set; }
        public WhisperSegment[]? segments { get; set; }
    }

    private class WhisperSegment
    {
        public double start { get; set; }
        public double end { get; set; }
        public string? text { get; set; }
    }
}