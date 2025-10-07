using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.DTOs.Transcription;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace YoutubeRag.Infrastructure.Services;

public class LocalWhisperService : ITranscriptionService
{
    private readonly ILogger<LocalWhisperService> _logger;
    private readonly IAppConfiguration _appConfiguration;
    private readonly string _whisperPath;

    public LocalWhisperService(
        ILogger<LocalWhisperService> logger,
        IAppConfiguration appConfiguration)
    {
        _logger = logger;
        _appConfiguration = appConfiguration;
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

    /// <inheritdoc/>
    public async Task<TranscriptionResultDto> TranscribeAudioAsync(TranscriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("Starting Whisper transcription for video: {VideoId}, Quality: {Quality}, Language: {Language}",
                request.VideoId, request.Quality, request.Language);

            // Check if the audio file exists
            if (!File.Exists(request.AudioFilePath))
            {
                throw new FileNotFoundException($"Audio file not found: {request.AudioFilePath}");
            }

            // Map quality to Whisper model size
            var initialModelSize = request.Quality switch
            {
                TranscriptionQuality.Low => "tiny",
                TranscriptionQuality.Medium => _appConfiguration.WhisperModelSize ?? "base",
                TranscriptionQuality.High => "medium",
                _ => "base"
            };

            // Execute transcription with automatic model downgrade on OOM
            var result = await TranscribeWithRetryAsync(
                request.VideoId,
                request.AudioFilePath,
                request.Language,
                initialModelSize,
                cancellationToken);

            var totalDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            var audioFileSize = new FileInfo(request.AudioFilePath).Length / (1024.0 * 1024.0); // MB

            _logger.LogInformation("Whisper transcription completed successfully for video: {VideoId} in {TotalDurationSeconds}s. Segments: {SegmentCount}, Duration: {VideoDuration}, AudioSize: {AudioSizeMB}MB, Model: {ModelUsed}, QualityDegraded: {QualityDegraded}, RetryAttempts: {RetryAttempts}",
                request.VideoId, totalDuration, result.Segments.Count, result.Duration, audioFileSize, result.ModelUsed, result.QualityDegraded, result.RetryAttempts);

            return result;
        }
        catch (Exception ex)
        {
            var failureDuration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogError(ex, "Whisper transcription failed for video: {VideoId} after {DurationSeconds}s. Error: {ErrorMessage}",
                request.VideoId, failureDuration, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes Whisper transcription with automatic model downgrade retry on OOM errors
    /// </summary>
    private async Task<TranscriptionResultDto> TranscribeWithRetryAsync(
        string videoId,
        string audioFilePath,
        string language,
        string initialModelSize,
        CancellationToken cancellationToken)
    {
        var currentModel = initialModelSize;
        var originalModel = initialModelSize;
        var retryAttempts = 0;
        var maxRetries = 3; // Maximum 3 model downgrades (e.g., small → base → tiny)

        while (retryAttempts <= maxRetries)
        {
            try
            {
                _logger.LogInformation("Attempting transcription for video: {VideoId} with model: {ModelSize} (Attempt {Attempt}/{MaxAttempts})",
                    videoId, currentModel, retryAttempts + 1, maxRetries + 1);

                var result = await ExecuteWhisperTranscriptionAsync(
                    videoId,
                    audioFilePath,
                    language,
                    currentModel,
                    cancellationToken);

                // Success! Set metadata
                result.ModelUsed = currentModel;
                result.OriginalModel = originalModel;
                result.QualityDegraded = currentModel != originalModel;
                result.RetryAttempts = retryAttempts;

                if (result.QualityDegraded)
                {
                    _logger.LogWarning("Transcription completed with downgraded model for video: {VideoId}. OriginalModel: {OriginalModel}, FinalModel: {FinalModel}, RetryAttempts: {RetryAttempts}",
                        videoId, originalModel, currentModel, retryAttempts);
                }

                return result;
            }
            catch (OutOfMemoryException ex)
            {
                retryAttempts++;
                _logger.LogWarning(ex, "OutOfMemoryException detected during transcription for video: {VideoId} with model: {ModelSize}. Attempt {Attempt}/{MaxAttempts}",
                    videoId, currentModel, retryAttempts, maxRetries + 1);

                if (!_appConfiguration.EnableAutoModelDowngrade)
                {
                    _logger.LogError("Auto model downgrade is disabled. Failing transcription for video: {VideoId}", videoId);
                    throw new InvalidOperationException($"Out of memory during transcription with model '{currentModel}' and auto-downgrade is disabled", ex);
                }

                // Try to downgrade model
                var nextModel = GetDowngradedModel(currentModel);
                if (string.IsNullOrEmpty(nextModel))
                {
                    _logger.LogError("Unable to downgrade model further from: {CurrentModel} for video: {VideoId}. All retry attempts exhausted.",
                        currentModel, videoId);
                    throw new InvalidOperationException($"Out of memory during transcription. Already using smallest model '{currentModel}'. Cannot downgrade further.", ex);
                }

                _logger.LogInformation("Downgrading Whisper model from {CurrentModel} to {NextModel} for video: {VideoId}",
                    currentModel, nextModel, videoId);

                currentModel = nextModel;
            }
            catch (InvalidOperationException ex) when (IsOutOfMemoryError(ex.Message))
            {
                retryAttempts++;
                _logger.LogWarning(ex, "Whisper OOM error detected in stderr for video: {VideoId} with model: {ModelSize}. Attempt {Attempt}/{MaxAttempts}. Error: {ErrorMessage}",
                    videoId, currentModel, retryAttempts, maxRetries + 1, ex.Message);

                if (!_appConfiguration.EnableAutoModelDowngrade)
                {
                    _logger.LogError("Auto model downgrade is disabled. Failing transcription for video: {VideoId}", videoId);
                    throw new InvalidOperationException($"Out of memory during transcription with model '{currentModel}' and auto-downgrade is disabled", ex);
                }

                // Try to downgrade model
                var nextModel = GetDowngradedModel(currentModel);
                if (string.IsNullOrEmpty(nextModel))
                {
                    _logger.LogError("Unable to downgrade model further from: {CurrentModel} for video: {VideoId}. All retry attempts exhausted.",
                        currentModel, videoId);
                    throw new InvalidOperationException($"Out of memory during transcription. Already using smallest model '{currentModel}'. Cannot downgrade further.", ex);
                }

                _logger.LogInformation("Downgrading Whisper model from {CurrentModel} to {NextModel} for video: {VideoId}",
                    currentModel, nextModel, videoId);

                currentModel = nextModel;
            }
        }

        // Should not reach here, but if we do, throw error
        throw new InvalidOperationException($"Failed to transcribe video {videoId} after {retryAttempts} retry attempts with model downgrades");
    }

    /// <summary>
    /// Executes Whisper transcription with the specified model
    /// </summary>
    private async Task<TranscriptionResultDto> ExecuteWhisperTranscriptionAsync(
        string videoId,
        string audioFilePath,
        string language,
        string modelSize,
        CancellationToken cancellationToken)
    {
        // Find Whisper executable
        var whisperPath = FindWhisperExecutable();
        if (string.IsNullOrEmpty(whisperPath))
        {
            _logger.LogError("Whisper executable not found for video: {VideoId}", videoId);
            throw new InvalidOperationException("Whisper executable not found. Please install whisper: pip install openai-whisper");
        }
        _logger.LogDebug("Using Whisper executable: {WhisperPath}, Model: {ModelSize}", whisperPath, modelSize);

        // Prepare output directory
        var outputDir = Path.GetDirectoryName(audioFilePath);
        var outputFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(audioFilePath)}.json");

        // Build Whisper command arguments
        var languageParam = language == "auto" || string.IsNullOrEmpty(language)
            ? ""
            : $"--language {language}";

        var arguments = $"\"{audioFilePath}\" --model {modelSize} --output_format json --output_dir \"{outputDir}\" --verbose False {languageParam}";

        // Execute Whisper
        var processInfo = new ProcessStartInfo
        {
            FileName = whisperPath,
            Arguments = arguments,
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
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Whisper process");
        }

        // Wait for process with timeout to prevent infinite hangs
        var whisperProcessStartTime = DateTime.UtcNow;
        string errorOutput = string.Empty;

        try
        {
            // Timeout: 60 minutes for transcription (adjust based on video length)
            var timeout = TimeSpan.FromMinutes(60);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            _logger.LogDebug("Waiting for Whisper process to complete for video: {VideoId} (timeout: {Timeout} minutes)",
                videoId, timeout.TotalMinutes);

            await process.WaitForExitAsync(cts.Token);

            var whisperProcessDuration = (DateTime.UtcNow - whisperProcessStartTime).TotalSeconds;
            _logger.LogInformation("Whisper process completed for video: {VideoId} with exit code: {ExitCode} in {DurationSeconds}s",
                videoId, process.ExitCode, whisperProcessDuration);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not user cancellation)
            _logger.LogError("Whisper process timed out after 60 minutes");

            if (!process.HasExited)
            {
                _logger.LogWarning("Killing hung Whisper process");
                process.Kill();
                await process.WaitForExitAsync(); // Wait for kill to complete
            }

            throw new TimeoutException($"Whisper transcription timed out after 60 minutes for video {videoId}");
        }

        // Read error output for OOM detection
        errorOutput = await process.StandardError.ReadToEndAsync();

        // Check exit code and capture error output
        if (process.ExitCode != 0)
        {
            _logger.LogError("Whisper failed for video: {VideoId} with exit code {ExitCode}: {Error}",
                videoId, process.ExitCode, errorOutput);

            // Check if this is an OOM error
            if (IsOutOfMemoryError(errorOutput))
            {
                throw new InvalidOperationException($"Whisper out of memory with model '{modelSize}': {errorOutput}");
            }

            throw new InvalidOperationException($"Whisper failed with exit code {process.ExitCode}: {errorOutput}");
        }

        // Parse the output JSON file
        var whisperResult = await ParseWhisperOutput(outputFile);

        // Convert to DTO
        var result = new TranscriptionResultDto
        {
            VideoId = videoId,
            Language = whisperResult.Language,
            Duration = whisperResult.Duration,
            Confidence = whisperResult.Confidence,
            Segments = whisperResult.Segments.Select(s => new TranscriptSegmentDto
            {
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Text = s.Text,
                Confidence = s.Confidence
            }).ToList()
        };

        // Clean up temporary JSON file
        if (File.Exists(outputFile))
        {
            try { File.Delete(outputFile); } catch { /* Ignore cleanup errors */ }
        }

        return result;
    }

    /// <summary>
    /// Determines if an error message indicates an out-of-memory condition
    /// </summary>
    private bool IsOutOfMemoryError(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return false;

        var oomIndicators = new[]
        {
            "out of memory",
            "outofmemory",
            "oom",
            "memory error",
            "memoryerror",
            "cannot allocate memory",
            "failed to allocate",
            "allocation failed",
            "cuda out of memory",
            "torch.cuda.outofmemoryerror",
            "RuntimeError: CUDA out of memory",
            "not enough memory"
        };

        var lowerError = errorMessage.ToLowerInvariant();
        return oomIndicators.Any(indicator => lowerError.Contains(indicator));
    }

    /// <summary>
    /// Returns the next smaller model in the downgrade chain, or null if already at smallest
    /// Model downgrade chain: large → medium → small → base → tiny → null
    /// </summary>
    private string? GetDowngradedModel(string currentModel)
    {
        return currentModel.ToLowerInvariant() switch
        {
            "large" => "medium",
            "medium" => "small",
            "small" => "base",
            "base" => "tiny",
            "tiny" => null, // Cannot downgrade further
            _ => null
        };
    }

    /// <inheritdoc/>
    public async Task<bool> IsWhisperAvailableAsync()
    {
        try
        {
            var whisperPath = FindWhisperExecutable();
            return await Task.FromResult(!string.IsNullOrEmpty(whisperPath));
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetWhisperVersionAsync()
    {
        try
        {
            var whisperPath = FindWhisperExecutable();
            if (string.IsNullOrEmpty(whisperPath))
            {
                return null;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = whisperPath,
                Arguments = "--version",
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
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Whisper version");
            return null;
        }
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
                // For absolute paths with .exe extension, check if file exists directly
                if (Path.IsPathRooted(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(path))
                    {
                        _logger.LogInformation("Found Whisper executable at: {Path}", path);
                        return path;
                    }
                    continue;
                }

                // For other paths, try to execute to verify
                var processInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--help",
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
                    // Whisper doesn't support --version, use --help which always works
                    if (process.ExitCode == 0 || process.ExitCode == 2)
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