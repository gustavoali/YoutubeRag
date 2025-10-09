using Microsoft.Extensions.Logging;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Services;

/// <summary>
/// Service for extracting audio from various video sources
/// </summary>
public class AudioExtractionService : IAudioExtractionService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly ILogger<AudioExtractionService> _logger;
    private readonly IAppConfiguration _appConfiguration;
    private readonly string _audioStoragePath;

    public AudioExtractionService(
        ILogger<AudioExtractionService> logger,
        IAppConfiguration appConfiguration)
    {
        _logger = logger;
        _appConfiguration = appConfiguration;
        _youtubeClient = new YoutubeClient();

        // Set up audio storage path
        _audioStoragePath = _appConfiguration.AudioStoragePath ?? Path.Combine(Directory.GetCurrentDirectory(), "data", "audio");

        // Ensure the directory exists
        if (!Directory.Exists(_audioStoragePath))
        {
            Directory.CreateDirectory(_audioStoragePath);
            _logger.LogInformation("Created audio storage directory: {Path}", _audioStoragePath);
        }
    }

    /// <inheritdoc/>
    public async Task<string> ExtractAudioFromYouTubeAsync(string youTubeId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Starting audio extraction from YouTube video: {YouTubeId}", youTubeId);

            // Try YoutubeExplode first
            try
            {
                // Get stream manifest
                _logger.LogDebug("Fetching stream manifest for YouTube video: {YouTubeId}", youTubeId);
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(youTubeId, cancellationToken);

                // Get the highest quality audio stream
                var audioStreamInfo = streamManifest
                    .GetAudioOnlyStreams()
                    .Where(s => s.Container == Container.Mp4 || s.Container == Container.WebM || s.Container == Container.Mp3)
                    .OrderByDescending(s => s.Bitrate)
                    .FirstOrDefault();

                _logger.LogDebug("Selected audio stream for {YouTubeId}: Bitrate={Bitrate}, Container={Container}, Size={SizeMB}MB",
                    youTubeId, audioStreamInfo?.Bitrate, audioStreamInfo?.Container, audioStreamInfo?.Size.MegaBytes);

                if (audioStreamInfo == null)
                {
                    throw new InvalidOperationException($"No suitable audio stream found for video: {youTubeId}");
                }

                // Generate file path
                var fileName = $"{youTubeId}_audio_{DateTime.UtcNow:yyyyMMddHHmmss}.{audioStreamInfo.Container.Name}";
                var filePath = Path.Combine(_audioStoragePath, fileName);

                // Check file size limit
                if (_appConfiguration.MaxAudioFileSizeMB > 0 && audioStreamInfo.Size.MegaBytes > _appConfiguration.MaxAudioFileSizeMB)
                {
                    throw new InvalidOperationException(
                        $"Audio file size ({audioStreamInfo.Size.MegaBytes:F2} MB) exceeds maximum allowed size ({_appConfiguration.MaxAudioFileSizeMB} MB)");
                }

                // Download the audio stream
                _logger.LogInformation("Downloading audio stream for {YouTubeId}. Size: {SizeMB}MB, Bitrate: {Bitrate}",
                    youTubeId, audioStreamInfo.Size.MegaBytes, audioStreamInfo.Bitrate);
                var downloadStartTime = DateTime.UtcNow;
                await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, filePath, cancellationToken: cancellationToken);
                var downloadDuration = (DateTime.UtcNow - downloadStartTime).TotalSeconds;
                var totalDuration = (DateTime.UtcNow - startTime).TotalSeconds;

                _logger.LogInformation("Audio extraction completed successfully for {YouTubeId} in {TotalDurationSeconds}s (download: {DownloadDurationSeconds}s). FilePath: {FilePath}, Size: {SizeMB}MB",
                    youTubeId, totalDuration, downloadDuration, filePath, audioStreamInfo.Size.MegaBytes);
                return filePath;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("403"))
            {
                var fallbackStartTime = DateTime.UtcNow;
                _logger.LogWarning(httpEx, "YoutubeExplode failed with 403 Forbidden for {YouTubeId}. Attempting fallback to yt-dlp", youTubeId);

                // Fall back to yt-dlp
                var result = await ExtractAudioUsingYtDlpAsync(youTubeId, cancellationToken);
                var fallbackDuration = (DateTime.UtcNow - fallbackStartTime).TotalSeconds;
                _logger.LogInformation("Fallback to yt-dlp successful for {YouTubeId} in {DurationSeconds}s", youTubeId, fallbackDuration);
                return result;
            }
        }
        catch (Exception ex)
        {
            var failureDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex, "Audio extraction failed for YouTube video: {YouTubeId} after {DurationSeconds}s. Error: {ErrorMessage}",
                youTubeId, failureDuration, ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ExtractAudioFromVideoFileAsync(string videoFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting audio from video file: {VideoPath}", videoFilePath);

            if (!File.Exists(videoFilePath))
            {
                throw new FileNotFoundException($"Video file not found: {videoFilePath}");
            }

            // Generate output file path
            var fileName = Path.GetFileNameWithoutExtension(videoFilePath);
            var outputFileName = $"{fileName}_audio_{DateTime.UtcNow:yyyyMMddHHmmss}.mp3";
            var outputPath = Path.Combine(_audioStoragePath, outputFileName);

            // Use FFmpeg to extract audio (if available)
            if (await IsFFmpegAvailableAsync())
            {
                await ExtractAudioUsingFFmpegAsync(videoFilePath, outputPath, cancellationToken);
            }
            else
            {
                // Fallback: copy the file if it's already an audio format
                var extension = Path.GetExtension(videoFilePath).ToLowerInvariant();
                if (extension == ".mp3" || extension == ".wav" || extension == ".m4a" || extension == ".flac")
                {
                    File.Copy(videoFilePath, outputPath, overwrite: true);
                    _logger.LogWarning("FFmpeg not available. Copied audio file directly: {OutputPath}", outputPath);
                }
                else
                {
                    throw new NotSupportedException("FFmpeg is required to extract audio from video files");
                }
            }

            _logger.LogInformation("Audio extracted successfully to: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract audio from video file: {VideoPath}", videoFilePath);
            throw;
        }
    }

    /// <summary>
    /// Extracts Whisper-compatible audio (16kHz mono WAV) from video file using FFmpeg
    /// </summary>
    /// <param name="videoFilePath">Path to the video file</param>
    /// <param name="videoId">Video ID for proper file naming and directory structure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the extracted WAV audio file</returns>
    public async Task<string> ExtractWhisperAudioFromVideoAsync(
        string videoFilePath,
        string videoId,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Extracting Whisper-compatible audio from video: {VideoPath}", videoFilePath);

            // Step 1: Validate input file
            if (!File.Exists(videoFilePath))
            {
                throw new FileNotFoundException($"Video file not found: {videoFilePath}");
            }

            var videoFileInfo = new FileInfo(videoFilePath);
            _logger.LogDebug("Video file size: {SizeMB:F2}MB", videoFileInfo.Length / (1024.0 * 1024.0));

            // Step 2: Verify FFmpeg availability
            if (!await IsFFmpegAvailableAsync())
            {
                throw new InvalidOperationException(
                    "FFmpeg is required for Whisper audio extraction but is not available. " +
                    "Please install FFmpeg and ensure it's in the system PATH.");
            }

            // Step 3: Generate output path (same directory as video)
            var videoDir = Path.GetDirectoryName(videoFilePath);
            if (string.IsNullOrEmpty(videoDir))
            {
                throw new InvalidOperationException($"Could not determine directory for video: {videoFilePath}");
            }

            var audioFileName = $"{videoId}_{DateTime.UtcNow:yyyyMMddHHmmss}.wav";
            var audioPath = Path.Combine(videoDir, audioFileName);

            _logger.LogDebug("Audio will be extracted to: {AudioPath}", audioPath);

            // Step 4: Extract audio with Whisper-optimized parameters
            await ExtractWhisperAudioUsingFFmpegAsync(videoFilePath, audioPath, cancellationToken);

            // Step 5: Verify output file
            if (!File.Exists(audioPath))
            {
                throw new InvalidOperationException(
                    $"FFmpeg completed but audio file not found: {audioPath}");
            }

            var audioFileInfo = new FileInfo(audioPath);
            if (audioFileInfo.Length == 0)
            {
                throw new InvalidOperationException(
                    $"FFmpeg completed but audio file is empty: {audioPath}");
            }

            var extractionDuration = (DateTime.UtcNow - startTime).TotalSeconds;

            _logger.LogInformation(
                "Whisper audio extracted successfully in {DurationSeconds:F2}s. " +
                "Path: {AudioPath}, Size: {SizeMB:F2}MB",
                extractionDuration, audioPath, audioFileInfo.Length / (1024.0 * 1024.0));

            // Step 6: Delete video file (cleanup intermediate)
            try
            {
                File.Delete(videoFilePath);
                _logger.LogInformation("Deleted intermediate video file: {VideoPath}", videoFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete video file: {VideoPath}. " +
                    "File will be cleaned up by recurring job.", videoFilePath);
                // Don't fail the operation if cleanup fails
            }

            return audioPath;
        }
        catch (Exception ex)
        {
            var failureDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex,
                "Failed to extract Whisper audio from video: {VideoPath} after {DurationSeconds:F2}s. " +
                "Error: {ErrorMessage}",
                videoFilePath, failureDuration, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Extracts audio from video file using FFmpeg with Whisper-optimized parameters
    /// </summary>
    /// <param name="inputPath">Path to input video file</param>
    /// <param name="outputPath">Path to output WAV audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ExtractWhisperAudioUsingFFmpegAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        // Whisper-optimized FFmpeg parameters:
        // -i: input file
        // -vn: disable video (audio only)
        // -acodec pcm_s16le: PCM 16-bit little-endian (required by Whisper)
        // -ar 16000: 16kHz sample rate (Whisper optimal rate)
        // -ac 1: mono audio (1 channel, Whisper doesn't need stereo)
        // -y: overwrite output file if exists
        var arguments = $"-i \"{inputPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputPath}\" -y";

        _logger.LogDebug("Running FFmpeg with Whisper-optimized parameters: {Arguments}", arguments);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var startTime = DateTime.UtcNow;
        process.Start();

        // Read stderr for FFmpeg progress/errors
        var stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        var duration = (DateTime.UtcNow - startTime).TotalSeconds;

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "FFmpeg failed with exit code {ExitCode} after {DurationSeconds:F2}s. " +
                "Input: {Input}, Output: {Output}, Error: {Error}",
                process.ExitCode, duration, inputPath, outputPath, stderr);

            throw new InvalidOperationException(
                $"FFmpeg failed with exit code {process.ExitCode}. " +
                $"This may indicate an invalid video file or codec issue. Error: {stderr}");
        }

        _logger.LogDebug(
            "FFmpeg completed successfully in {DurationSeconds:F2}s. Output: {Output}",
            duration, outputPath);

        // Verify output file was created and has content
        var fileInfo = new FileInfo(outputPath);
        if (!fileInfo.Exists)
        {
            throw new InvalidOperationException(
                $"FFmpeg completed but output file not found: {outputPath}");
        }

        if (fileInfo.Length == 0)
        {
            throw new InvalidOperationException(
                $"FFmpeg completed but output file is empty: {outputPath}");
        }

        _logger.LogInformation(
            "FFmpeg audio extraction successful. Output size: {SizeMB:F2}MB, Duration: {DurationSeconds:F2}s",
            fileInfo.Length / (1024.0 * 1024.0), duration);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAudioFileAsync(string audioFilePath)
    {
        try
        {
            if (!File.Exists(audioFilePath))
            {
                _logger.LogWarning("Audio file not found for deletion: {FilePath}", audioFilePath);
                return false;
            }

            await Task.Run(() => File.Delete(audioFilePath));
            _logger.LogInformation("Deleted audio file: {FilePath}", audioFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete audio file: {FilePath}", audioFilePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<AudioInfo> GetAudioInfoAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
            }

            var fileInfo = new FileInfo(audioFilePath);
            var audioInfo = new AudioInfo
            {
                FileSizeBytes = fileInfo.Length,
                Format = Path.GetExtension(audioFilePath).TrimStart('.')
            };

            // Try to get detailed info using FFprobe if available
            if (await IsFFprobeAvailableAsync())
            {
                await GetAudioInfoUsingFFprobeAsync(audioFilePath, audioInfo, cancellationToken);
            }
            else
            {
                // Set default values if FFprobe is not available
                audioInfo.Duration = TimeSpan.Zero;
                audioInfo.SampleRate = 44100; // Common default
                audioInfo.Channels = 2; // Stereo default
                audioInfo.Bitrate = 128000; // 128 kbps default
                _logger.LogWarning("FFprobe not available. Using default audio info values.");
            }

            return audioInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audio info: {FilePath}", audioFilePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAudioFileAsync(string audioFilePath)
    {
        try
        {
            if (!File.Exists(audioFilePath))
            {
                return false;
            }

            var fileInfo = new FileInfo(audioFilePath);
            if (fileInfo.Length == 0)
            {
                return false;
            }

            // Check if it's a supported audio format
            var supportedFormats = new[] { ".mp3", ".wav", ".m4a", ".flac", ".webm", ".mp4", ".ogg" };
            var extension = Path.GetExtension(audioFilePath).ToLowerInvariant();

            return await Task.FromResult(supportedFormats.Contains(extension));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate audio file: {FilePath}", audioFilePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public string GetAudioStoragePath()
    {
        return _audioStoragePath;
    }

    private async Task<bool> IsFFmpegAvailableAsync()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsFFprobeAvailableAsync()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task ExtractAudioUsingFFmpegAsync(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputPath}\" -vn -acodec mp3 -ab 192k -ar 44100 \"{outputPath}\" -y",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {error}");
        }
    }

    private async Task GetAudioInfoUsingFFprobeAsync(string audioPath, AudioInfo audioInfo, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration,bit_rate:stream=sample_rate,channels -of json \"{audioPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                // Parse JSON output from FFprobe
                // For simplicity, we'll use basic parsing here
                // In production, you might want to use a JSON library

                if (output.Contains("duration"))
                {
                    var durationMatch = System.Text.RegularExpressions.Regex.Match(output, "\"duration\":\\s*\"([0-9.]+)\"");
                    if (durationMatch.Success && double.TryParse(durationMatch.Groups[1].Value, out var seconds))
                    {
                        audioInfo.Duration = TimeSpan.FromSeconds(seconds);
                    }
                }

                if (output.Contains("bit_rate"))
                {
                    var bitrateMatch = System.Text.RegularExpressions.Regex.Match(output, "\"bit_rate\":\\s*\"([0-9]+)\"");
                    if (bitrateMatch.Success && int.TryParse(bitrateMatch.Groups[1].Value, out var bitrate))
                    {
                        audioInfo.Bitrate = bitrate;
                    }
                }

                if (output.Contains("sample_rate"))
                {
                    var sampleRateMatch = System.Text.RegularExpressions.Regex.Match(output, "\"sample_rate\":\\s*\"([0-9]+)\"");
                    if (sampleRateMatch.Success && int.TryParse(sampleRateMatch.Groups[1].Value, out var sampleRate))
                    {
                        audioInfo.SampleRate = sampleRate;
                    }
                }

                if (output.Contains("channels"))
                {
                    var channelsMatch = System.Text.RegularExpressions.Regex.Match(output, "\"channels\":\\s*([0-9]+)");
                    if (channelsMatch.Success && int.TryParse(channelsMatch.Groups[1].Value, out var channels))
                    {
                        audioInfo.Channels = channels;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get detailed audio info using FFprobe");
        }
    }

    private async Task<bool> IsYtDlpAvailableAsync()
    {
        try
        {
            var ytDlpPath = FindYtDlpExecutable();
            if (string.IsNullOrEmpty(ytDlpPath))
            {
                return false;
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string FindYtDlpExecutable()
    {
        // Common yt-dlp locations
        var possiblePaths = new[]
        {
            "yt-dlp",
            "yt-dlp.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Packages", "PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0", "LocalCache", "local-packages", "Python313", "Scripts", "yt-dlp.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python313", "Scripts", "yt-dlp.exe"),
            "/usr/local/bin/yt-dlp",
            "/usr/bin/yt-dlp"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                // For absolute paths with .exe extension, check if file exists directly
                if (Path.IsPathRooted(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(path))
                    {
                        _logger.LogInformation("Found yt-dlp executable at: {Path}", path);
                        return path;
                    }
                    continue;
                }

                // For simple commands, try to execute
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit(2000);
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Found yt-dlp executable at: {Path}", path);
                    return path;
                }
            }
            catch
            {
                // Continue searching
            }
        }

        _logger.LogWarning("yt-dlp executable not found. Install with: pip install yt-dlp");
        return string.Empty;
    }

    private async Task<string> ExtractAudioUsingYtDlpAsync(string youTubeId, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var ytDlpPath = FindYtDlpExecutable();
        if (string.IsNullOrEmpty(ytDlpPath))
        {
            _logger.LogError("yt-dlp executable not found for video: {YouTubeId}", youTubeId);
            throw new InvalidOperationException("yt-dlp is not available. Please install it with: pip install yt-dlp");
        }

        try
        {
            _logger.LogInformation("Starting audio extraction using yt-dlp for video: {YouTubeId}", youTubeId);

            // Generate output file path
            var fileName = $"{youTubeId}_audio_{DateTime.UtcNow:yyyyMMddHHmmss}.mp3";
            var outputTemplate = Path.Combine(_audioStoragePath, fileName);

            // Build yt-dlp command
            // -x: extract audio
            // --audio-format mp3: convert to mp3
            // --audio-quality 0: best quality
            // -o: output template
            var videoUrl = $"https://www.youtube.com/watch?v={youTubeId}";
            var arguments = $"-x --audio-format mp3 --audio-quality 0 -o \"{outputTemplate}\" \"{videoUrl}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set environment variables for Unicode support
            processInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            processInfo.Environment["PYTHONUTF8"] = "1";

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start yt-dlp process");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            // Wait with timeout (30 minutes for audio download)
            var timeout = TimeSpan.FromMinutes(30);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("yt-dlp process timed out after {Timeout} minutes", timeout.TotalMinutes);
                if (!process.HasExited)
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                throw new TimeoutException($"yt-dlp audio extraction timed out after {timeout.TotalMinutes} minutes for video {youTubeId}");
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("yt-dlp failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
                throw new InvalidOperationException($"yt-dlp failed with exit code {process.ExitCode}: {error}");
            }

            // Verify file was created
            if (!File.Exists(outputTemplate))
            {
                _logger.LogError("yt-dlp completed but output file not found for {YouTubeId}: {OutputPath}", youTubeId, outputTemplate);
                throw new InvalidOperationException($"yt-dlp completed but output file not found: {outputTemplate}");
            }

            var fileInfo = new FileInfo(outputTemplate);
            var totalDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation("Audio extraction completed using yt-dlp for {YouTubeId} in {DurationSeconds}s. FilePath: {FilePath}, Size: {SizeMB}MB",
                youTubeId, totalDuration, outputTemplate, fileInfo.Length / (1024.0 * 1024.0));
            return outputTemplate;
        }
        catch (Exception ex)
        {
            var failureDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex, "yt-dlp audio extraction failed for video: {YouTubeId} after {DurationSeconds}s. Error: {ErrorMessage}",
                youTubeId, failureDuration, ex.Message);
            throw;
        }
    }
}