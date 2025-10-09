# Epic 3 - Gaps Identificados

**Epic:** YRUS-0103 - Descargar Video y Extraer Audio
**Fecha de Validación:** 9 de Octubre, 2025
**Archivo Validado:** `YoutubeRag.Infrastructure/Services/AudioExtractionService.cs`
**Validador:** Backend Developer Agent

---

## Resumen Ejecutivo

El `AudioExtractionService` está **implementado parcialmente** con funcionalidad core completa para descarga de audio desde YouTube, pero **NO cumple varios AC críticos** de YRUS-0103 relacionados con:

- Descarga de video separada (MP4)
- Extracción de audio con FFmpeg específicamente normalizado para Whisper (16kHz mono WAV)
- Actualización de estados de Video durante el pipeline
- Progress tracking granular durante descarga
- Integración con Job.ProgressPercentage
- Verificación de espacio en disco
- Cleanup automático con Hangfire recurring job

**Implementación actual:** ~45% completo respecto a los AC de YRUS-0103

---

## Implementación Actual

### Funcionalidad IMPLEMENTADA

El servicio actual `AudioExtractionService` proporciona:

1. **Descarga directa de audio** usando YoutubeExplode (no descarga video intermedio)
2. **Fallback a yt-dlp** cuando YoutubeExplode falla con 403
3. **Conversión a MP3** (NO a WAV 16kHz mono como requiere Whisper)
4. **Gestión básica de archivos** en directorio `data/audio`
5. **Validación de archivos** de audio
6. **Cleanup manual** vía `DeleteAudioFileAsync()`
7. **Audio info extraction** usando FFprobe

### Arquitectura Actual

```
TranscriptionJobProcessor
    └─> AudioExtractionService.ExtractAudioFromYouTubeAsync()
            ├─> YoutubeClient.Videos.Streams.DownloadAsync() [Audio stream directo]
            └─> (Fallback) ExtractAudioUsingYtDlpAsync() [Si falla YoutubeExplode]
```

**PROBLEMA:** El AC1 y AC2 de YRUS-0103 especifican un flujo diferente:

```
REQUERIDO por AC:
1. Descargar VIDEO (MP4) completo con streaming
2. Extraer AUDIO (WAV 16kHz mono) del video usando FFmpeg
3. Eliminar archivo MP4 después de extracción
```

**ACTUAL:**
1. Descargar AUDIO directamente (MP3/WebM/MP4)
2. No hay extracción separada con FFmpeg
3. No hay normalización a 16kHz mono WAV

---

## Gaps Encontrados

### GAP 1: Descarga de Video Intermedia NO Implementada

**AC afectado:** AC1 - Descarga de Video con Streaming

**Descripción:**
El sistema actual descarga audio directamente, pero los AC especifican descargar el **video completo (MP4)** primero y luego extraer audio. Esto permite:
- Mayor control sobre formato de salida
- Mejor manejo de progreso (tamaño conocido)
- Normalización consistente con FFmpeg
- Reusabilidad si se necesita video en el futuro

**Código faltante:**
- Método `DownloadVideoAsync()` que descargue stream de video MP4
- Implementación de `IVideoDownloadService` (interfaz existe pero sin implementación)
- Progress tracking durante descarga de video

**Ubicación:**
- Crear: `YoutubeRag.Infrastructure/Services/VideoDownloadService.cs`
- Implementar interfaz: `YoutubeRag.Application/Interfaces/IVideoDownloadService.cs` (ya existe)

**Prioridad:** P0 (Crítico)

**Esfuerzo:** 4 horas

**Implementación sugerida:**

```csharp
public class VideoDownloadService : IVideoDownloadService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly ITempFileManagementService _tempFileService;
    private readonly ILogger<VideoDownloadService> _logger;

    public async Task<string> DownloadVideoAsync(
        string youTubeId,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Get stream manifest
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(youTubeId, cancellationToken);

        // 2. Select highest quality muxed stream (video + audio)
        var streamInfo = streamManifest
            .GetMuxedStreams()
            .OrderByDescending(s => s.VideoQuality.MaxHeight)
            .FirstOrDefault();

        if (streamInfo == null)
        {
            throw new InvalidOperationException($"No suitable video stream found for: {youTubeId}");
        }

        // 3. Check disk space
        if (!await _tempFileService.HasSufficientDiskSpaceAsync(streamInfo.Size.Bytes * 2))
        {
            throw new InsufficientDiskSpaceException($"Insufficient disk space for video: {youTubeId}");
        }

        // 4. Generate file path
        var filePath = _tempFileService.GenerateFilePath(youTubeId, ".mp4");

        // 5. Download with progress tracking
        await _youtubeClient.Videos.Streams.DownloadAsync(
            streamInfo,
            filePath,
            progress,
            cancellationToken);

        return filePath;
    }

    public async Task<string> DownloadVideoWithDetailsAsync(
        string youTubeId,
        IProgress<VideoDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Implementation with detailed progress (bytes downloaded, speed, ETA)
        var startTime = DateTime.UtcNow;
        var lastReportTime = startTime;
        long totalBytes = 0;
        long downloadedBytes = 0;

        var simpleProgress = new Progress<double>(percentage =>
        {
            downloadedBytes = (long)(totalBytes * percentage);
            var elapsed = DateTime.UtcNow - startTime;
            var bytesPerSecond = elapsed.TotalSeconds > 0
                ? downloadedBytes / elapsed.TotalSeconds
                : 0;
            var remainingBytes = totalBytes - downloadedBytes;
            var eta = bytesPerSecond > 0
                ? TimeSpan.FromSeconds(remainingBytes / bytesPerSecond)
                : (TimeSpan?)null;

            // Report every 10 seconds to avoid spam
            if ((DateTime.UtcNow - lastReportTime).TotalSeconds >= 10)
            {
                progress?.Report(new VideoDownloadProgress
                {
                    Percentage = percentage * 100,
                    BytesDownloaded = downloadedBytes,
                    TotalBytes = totalBytes,
                    BytesPerSecond = bytesPerSecond,
                    EstimatedTimeRemaining = eta
                });
                lastReportTime = DateTime.UtcNow;
            }
        });

        return await DownloadVideoAsync(youTubeId, simpleProgress, cancellationToken);
    }
}
```

**Impacto en otros componentes:**
- Modificar `TranscriptionJobProcessor` para usar `IVideoDownloadService` antes de `IAudioExtractionService`
- Actualizar flujo: Download Video → Extract Audio → Delete Video

---

### GAP 2: Extracción de Audio NO Normalizada para Whisper

**AC afectado:** AC2 - Extracción de Audio con FFmpeg

**Descripción:**
El código actual:
- Descarga audio directo en MP3/WebM (línea 54-59)
- Usa FFmpeg solo para conversión de archivos locales (línea 131-133)
- NO normaliza a **16kHz mono WAV** como requiere Whisper

**Parámetros FFmpeg requeridos por AC2:**
```bash
ffmpeg -i input.mp4 -vn -acodec pcm_s16le -ar 16000 -ac 1 output.wav
```

**Parámetros FFmpeg actuales (línea 319):**
```bash
ffmpeg -i input -vn -acodec mp3 -ab 192k -ar 44100 output
```

**Diferencias críticas:**
| Parámetro | Requerido | Actual | Impacto |
|-----------|-----------|--------|---------|
| Codec | `pcm_s16le` (WAV) | `mp3` | Whisper requiere WAV sin pérdida |
| Sample Rate | `16000` (16kHz) | `44100` (44.1kHz) | Whisper optimizado para 16kHz |
| Channels | `1` (mono) | Default (stereo) | Whisper no necesita stereo |

**Código faltante:**
- Método que extraiga audio de MP4 a WAV 16kHz mono
- Integración con `IVideoDownloadService` primero
- Eliminación de MP4 después de extracción exitosa

**Ubicación:**
- Modificar: `YoutubeRag.Infrastructure/Services/AudioExtractionService.cs`
- Agregar método: `ExtractWhisperAudioFromVideoAsync(string videoFilePath)`

**Prioridad:** P0 (Crítico - afecta calidad de transcripción)

**Esfuerzo:** 3 horas

**Implementación sugerida:**

```csharp
/// <summary>
/// Extracts audio from video file normalized for Whisper (16kHz mono WAV)
/// </summary>
public async Task<string> ExtractWhisperAudioFromVideoAsync(
    string videoFilePath,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Extracting Whisper-optimized audio from video: {VideoPath}", videoFilePath);

        if (!File.Exists(videoFilePath))
        {
            throw new FileNotFoundException($"Video file not found: {videoFilePath}");
        }

        // Generate WAV output path
        var videoFileName = Path.GetFileNameWithoutExtension(videoFilePath);
        var outputFileName = $"{videoFileName}_whisper.wav";
        var outputPath = Path.Combine(_audioStoragePath, outputFileName);

        // Verify FFmpeg is available
        if (!await IsFFmpegAvailableAsync())
        {
            throw new InvalidOperationException("FFmpeg is required for audio extraction but is not available");
        }

        // Extract audio with Whisper-optimized parameters
        await ExtractWhisperAudioUsingFFmpegAsync(videoFilePath, outputPath, cancellationToken);

        _logger.LogInformation("Whisper audio extracted successfully: {OutputPath}", outputPath);

        // Clean up video file after successful extraction
        try
        {
            File.Delete(videoFilePath);
            _logger.LogInformation("Deleted video file after audio extraction: {VideoPath}", videoFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete video file: {VideoPath}", videoFilePath);
            // Don't fail the operation if cleanup fails
        }

        return outputPath;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to extract Whisper audio from video: {VideoPath}", videoFilePath);
        throw;
    }
}

private async Task ExtractWhisperAudioUsingFFmpegAsync(
    string inputPath,
    string outputPath,
    CancellationToken cancellationToken)
{
    // Whisper-optimized FFmpeg parameters
    // -vn: no video
    // -acodec pcm_s16le: PCM 16-bit little-endian (WAV)
    // -ar 16000: 16kHz sample rate (Whisper optimal)
    // -ac 1: mono audio
    var arguments = $"-i \"{inputPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputPath}\" -y";

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

    _logger.LogDebug("Running FFmpeg with args: {Arguments}", arguments);

    process.Start();

    var stderr = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync(cancellationToken);

    if (process.ExitCode != 0)
    {
        _logger.LogError("FFmpeg failed with exit code {ExitCode}. Error: {Error}",
            process.ExitCode, stderr);
        throw new InvalidOperationException(
            $"FFmpeg failed with exit code {process.ExitCode}: {stderr}");
    }

    // Verify output file was created and has content
    var fileInfo = new FileInfo(outputPath);
    if (!fileInfo.Exists || fileInfo.Length == 0)
    {
        throw new InvalidOperationException(
            $"FFmpeg completed but output file is invalid: {outputPath}");
    }

    _logger.LogInformation("FFmpeg conversion successful. Output size: {SizeMB}MB",
        fileInfo.Length / (1024.0 * 1024.0));
}
```

**Actualización del flujo en TranscriptionJobProcessor:**

```csharp
// Línea 111-128 actual: Solo extrae audio
audioFilePath = await _audioExtractionService.ExtractAudioFromYouTubeAsync(
    video.YouTubeId, cancellationToken);

// CAMBIAR A:

// Step 4A: Download video
var videoFilePath = await _videoDownloadService.DownloadVideoAsync(
    video.YouTubeId,
    progress: null, // TODO: Connect to Job progress
    cancellationToken);

// Step 4B: Extract audio from video (Whisper-optimized)
audioFilePath = await _audioExtractionService.ExtractWhisperAudioFromVideoAsync(
    videoFilePath, cancellationToken);
```

---

### GAP 3: NO Actualiza VideoStatus Durante Pipeline

**AC afectado:** AC1, AC2

**Descripción:**
El servicio `AudioExtractionService` NO actualiza el campo `Video.ProcessingStatus` durante la descarga y extracción.

**Estados esperados por AC:**
- AC1: `VideoStatus = Downloading` durante descarga de video
- AC2: `VideoStatus = AudioExtracted` después de extracción exitosa

**Código faltante:**
- Integración con `IVideoRepository` en `AudioExtractionService`
- Actualización de estado antes/después de operaciones

**Ubicación:**
- `YoutubeRag.Infrastructure/Services/VideoDownloadService.cs` (nuevo)
- `YoutubeRag.Infrastructure/Services/AudioExtractionService.cs` (modificar)

**Prioridad:** P1 (Alto - importante para tracking)

**Esfuerzo:** 2 horas

**Implementación sugerida:**

```csharp
public class VideoDownloadService : IVideoDownloadService
{
    private readonly IVideoRepository _videoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<string> DownloadVideoAsync(
        string youTubeId,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Get video entity
        var video = await _videoRepository.GetByYouTubeIdAsync(youTubeId);
        if (video == null)
        {
            throw new InvalidOperationException($"Video not found: {youTubeId}");
        }

        // Update status to Downloading
        video.ProcessingStatus = VideoStatus.Downloading;
        video.UpdatedAt = DateTime.UtcNow;
        await _videoRepository.UpdateAsync(video);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Download video...
            var filePath = await DownloadVideoInternalAsync(youTubeId, progress, cancellationToken);

            return filePath;
        }
        catch (Exception ex)
        {
            // Revert status on failure
            video.ProcessingStatus = VideoStatus.Failed;
            video.UpdatedAt = DateTime.UtcNow;
            await _videoRepository.UpdateAsync(video);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}

public class AudioExtractionService : IAudioExtractionService
{
    private readonly IVideoRepository _videoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<string> ExtractWhisperAudioFromVideoAsync(
        string videoFilePath,
        string youTubeId, // Add parameter
        CancellationToken cancellationToken = default)
    {
        var video = await _videoRepository.GetByYouTubeIdAsync(youTubeId);

        // Extract audio...
        var audioPath = await ExtractAudioInternalAsync(videoFilePath, cancellationToken);

        // Update status to AudioExtracted
        if (video != null)
        {
            video.ProcessingStatus = VideoStatus.AudioExtracted;
            video.UpdatedAt = DateTime.UtcNow;
            await _videoRepository.UpdateAsync(video);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return audioPath;
    }
}
```

**NOTA:** Actualmente `TranscriptionJobProcessor` actualiza `VideoStatus` al final (línea 210), pero NO durante las etapas intermedias.

---

### GAP 4: NO Hay Progress Tracking Durante Descarga

**AC afectado:** AC1, AC4 - Tracking de Progreso

**Descripción:**
El servicio NO reporta progreso granular durante la descarga de video/audio al sistema de Job tracking.

**Funcionalidad faltante:**
- Reporte cada 10 segundos del % descargado
- Actualización de `Job.ProgressPercentage`
- Cálculo de tiempo restante estimado
- Emisión de eventos a SignalR hub

**Código faltante:**
- Conexión de `IProgress<double>` a `IProgressNotificationService`
- Wrapper que convierte progreso de descarga a JobProgressDto

**Ubicación:**
- `YoutubeRag.Application/Services/TranscriptionJobProcessor.cs` (modificar línea 111-128)

**Prioridad:** P1 (Alto - UX importante)

**Esfuerzo:** 3 horas

**Implementación sugerida:**

```csharp
// En TranscriptionJobProcessor.ProcessTranscriptionJobAsync()

// Step 4A: Download video with progress tracking
var downloadProgress = new Progress<VideoDownloadProgress>(async progressInfo =>
{
    // Only report every 10 seconds to avoid spam
    if (_lastProgressReport == null ||
        (DateTime.UtcNow - _lastProgressReport.Value).TotalSeconds >= 10)
    {
        var overallProgress = CalculateOverallProgress(
            stage: "download",
            stageProgress: progressInfo.Percentage);

        await _progressNotificationService.NotifyJobProgressAsync(
            transcriptionJob.Id,
            new JobProgressDto
            {
                JobId = transcriptionJob.Id,
                VideoId = videoId,
                JobType = "Transcription",
                Status = "Running",
                Progress = overallProgress, // 0-100 overall
                CurrentStage = "Downloading video",
                StatusMessage = $"Downloaded {progressInfo.FormattedProgress} at {progressInfo.FormattedSpeed}",
                EstimatedTimeRemaining = progressInfo.EstimatedTimeRemaining,
                UpdatedAt = DateTime.UtcNow
            });

        // Update Job entity
        transcriptionJob.Progress = overallProgress;
        transcriptionJob.UpdatedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(transcriptionJob);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _lastProgressReport = DateTime.UtcNow;
    }
});

var videoFilePath = await _videoDownloadService.DownloadVideoWithDetailsAsync(
    video.YouTubeId,
    downloadProgress,
    cancellationToken);

// Helper method
private int CalculateOverallProgress(string stage, double stageProgress)
{
    // Stage weights as per AC4
    // Download: 10-30% (20% range)
    // Audio Extraction: 30-40% (10% range)
    // Transcription: 40-85% (45% range)
    // Save Segments: 85-100% (15% range)

    return stage switch
    {
        "download" => (int)(10 + (stageProgress * 0.20)),
        "extract_audio" => (int)(30 + (stageProgress * 0.10)),
        "transcribe" => (int)(40 + (stageProgress * 0.45)),
        "save_segments" => (int)(85 + (stageProgress * 0.15)),
        _ => 0
    };
}
```

**Actualmente:** `TranscriptionJobProcessor` reporta progreso solo en etapas discretas (línea 90, 116, 137, 155, 181, 195), pero NO durante la descarga continua.

---

### GAP 5: NO Verifica Espacio en Disco Antes de Descargar

**AC afectado:** AC3 - Gestión de Archivos Temporales

**Descripción:**
No hay verificación de espacio disponible antes de iniciar descarga, lo que puede causar fallas a mitad de descarga.

**Funcionalidad faltante:**
- Check de `DriveInfo.AvailableSpace` antes de descarga
- Comparación con tamaño estimado del video (+ buffer 2x)
- Error descriptivo si insuficiente

**Código faltante:**
- Implementación de `ITempFileManagementService.HasSufficientDiskSpaceAsync()`
- Llamada antes de iniciar descarga

**Ubicación:**
- `YoutubeRag.Infrastructure/Services/TempFileManagementService.cs` (archivo existe pero está vacío)
- `YoutubeRag.Infrastructure/Services/VideoDownloadService.cs` (nuevo)

**Prioridad:** P1 (Alto - previene fallas costosas)

**Esfuerzo:** 2 horas

**Implementación sugerida:**

```csharp
// TempFileManagementService.cs - IMPLEMENTAR

public class TempFileManagementService : ITempFileManagementService
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly ILogger<TempFileManagementService> _logger;
    private readonly string _baseTempPath;

    public TempFileManagementService(
        IAppConfiguration appConfiguration,
        ILogger<TempFileManagementService> logger)
    {
        _appConfiguration = appConfiguration;
        _logger = logger;
        _baseTempPath = appConfiguration.TempFilePath
            ?? Path.Combine(Path.GetTempPath(), "YoutubeRag");

        // Ensure directory exists
        if (!Directory.Exists(_baseTempPath))
        {
            Directory.CreateDirectory(_baseTempPath);
        }
    }

    public async Task<bool> HasSufficientDiskSpaceAsync(
        long requiredSizeBytes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var availableSpace = await GetAvailableDiskSpaceAsync();
            var requiredWithBuffer = requiredSizeBytes * 2; // 2x buffer for safety

            _logger.LogDebug(
                "Disk space check: Required={RequiredMB}MB (with buffer={BufferMB}MB), Available={AvailableMB}MB",
                requiredSizeBytes / (1024.0 * 1024.0),
                requiredWithBuffer / (1024.0 * 1024.0),
                availableSpace / (1024.0 * 1024.0));

            return availableSpace >= requiredWithBuffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check disk space");
            return false;
        }
    }

    public async Task<long> GetAvailableDiskSpaceAsync()
    {
        return await Task.Run(() =>
        {
            var drive = new DriveInfo(Path.GetPathRoot(_baseTempPath));
            return drive.AvailableFreeSpace;
        });
    }

    public string CreateVideoDirectory(string videoId)
    {
        var videoDir = Path.Combine(_baseTempPath, videoId);
        if (!Directory.Exists(videoDir))
        {
            Directory.CreateDirectory(videoDir);
            _logger.LogInformation("Created video directory: {Path}", videoDir);
        }
        return videoDir;
    }

    public string GenerateFilePath(string videoId, string extension)
    {
        var videoDir = CreateVideoDirectory(videoId);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"{videoId}_{timestamp}{extension}";
        return Path.Combine(videoDir, fileName);
    }

    public async Task<int> CleanupOldFilesAsync(
        int olderThanHours,
        CancellationToken cancellationToken = default)
    {
        // Implementation for cleanup job
        var deletedCount = 0;
        var cutoffDate = DateTime.UtcNow.AddHours(-olderThanHours);

        foreach (var videoDir in Directory.GetDirectories(_baseTempPath))
        {
            foreach (var file in Directory.GetFiles(videoDir))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted old file: {FilePath}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete file: {FilePath}", file);
                    }
                }
            }

            // Delete empty directories
            if (!Directory.GetFiles(videoDir).Any())
            {
                try
                {
                    Directory.Delete(videoDir);
                    _logger.LogDebug("Deleted empty directory: {DirPath}", videoDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete directory: {DirPath}", videoDir);
                }
            }
        }

        _logger.LogInformation("Cleanup completed. Deleted {Count} old files", deletedCount);
        return deletedCount;
    }

    // Implement other interface methods...
}
```

**Uso en VideoDownloadService:**

```csharp
public async Task<string> DownloadVideoAsync(string youTubeId, ...)
{
    var streamInfo = await GetBestStreamAsync(youTubeId);

    // Check disk space before downloading
    if (!await _tempFileService.HasSufficientDiskSpaceAsync(streamInfo.Size.Bytes))
    {
        var required = streamInfo.Size.Bytes * 2;
        var available = await _tempFileService.GetAvailableDiskSpaceAsync();

        throw new InsufficientDiskSpaceException(
            $"Insufficient disk space. Required: {FormatBytes(required)}, " +
            $"Available: {FormatBytes(available)}");
    }

    // Proceed with download...
}
```

---

### GAP 6: NO Hay Hangfire Recurring Job para Cleanup

**AC afectado:** AC3 - Gestión de Archivos Temporales

**Descripción:**
No existe un job recurrente de Hangfire que limpie archivos temporales >24 horas automáticamente.

**Funcionalidad faltante:**
- `TempFileCleanupJob` que ejecute cleanup
- Registro en `RecurringJobsSetup`
- Configuración de frecuencia (hourly o daily)

**Código faltante:**
- Clase `TempFileCleanupJob`
- Registro en `RecurringJobsSetup.ConfigureRecurringJobs()`

**Ubicación:**
- Crear: `YoutubeRag.Infrastructure/Jobs/TempFileCleanupJob.cs`
- Modificar: `YoutubeRag.Infrastructure/Jobs/RecurringJobsSetup.cs`

**Prioridad:** P1 (Alto - evita acumulación de archivos)

**Esfuerzo:** 2 horas

**Implementación sugerida:**

```csharp
// TempFileCleanupJob.cs - CREAR

using Hangfire;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job for cleaning up old temporary files
/// </summary>
public class TempFileCleanupJob
{
    private readonly ITempFileManagementService _tempFileService;
    private readonly IAppConfiguration _appConfiguration;
    private readonly ILogger<TempFileCleanupJob> _logger;

    public TempFileCleanupJob(
        ITempFileManagementService tempFileService,
        IAppConfiguration appConfiguration,
        ILogger<TempFileCleanupJob> logger)
    {
        _tempFileService = tempFileService;
        _appConfiguration = appConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Executes cleanup of old temporary files
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting temp file cleanup job");

            // Get cleanup age from configuration (default 24 hours)
            var cleanupAfterHours = _appConfiguration.CleanupAfterHours ?? 24;

            // Execute cleanup
            var deletedCount = await _tempFileService.CleanupOldFilesAsync(
                cleanupAfterHours,
                cancellationToken);

            // Get storage stats after cleanup
            var stats = await _tempFileService.GetStorageStatsAsync();

            _logger.LogInformation(
                "Temp file cleanup completed. Deleted {Count} files. " +
                "Current stats: {TotalFiles} files, {TotalSize}, {AvailableSpace} available",
                deletedCount,
                stats.TotalFiles,
                stats.FormattedTotalSize,
                stats.FormattedAvailableSpace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temp file cleanup job");
            throw;
        }
    }
}
```

**Registro en RecurringJobsSetup.cs:**

```csharp
public static void ConfigureRecurringJobs(IRecurringJobManager recurringJobManager)
{
    // ... existing jobs ...

    // Clean up old temporary files every hour
    recurringJobManager.AddOrUpdate<TempFileCleanupJob>(
        "cleanup-temp-files",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Hourly); // Run every hour

    // OR daily at specific time:
    // Cron.Daily(4, 0) // 4:00 AM daily
}
```

**Configuración en appsettings.json:**

```json
{
  "Processing": {
    "TempFilePath": "C:\\Temp\\YoutubeRag",
    "CleanupAfterHours": 24,
    "MinDiskSpaceGB": 5
  }
}
```

---

### GAP 7: NO Implementa Retry Logic para Network Errors

**AC afectado:** AC5 - Manejo de Errores y Retry

**Descripción:**
No hay retry automático para errores de red durante descarga. El fallback a yt-dlp existe (línea 92-102) pero NO hay retry con backoff exponencial.

**Funcionalidad faltante:**
- Retry con Polly (3 intentos)
- Exponential backoff (10s, 30s, 90s)
- Logging de intentos de retry

**Código faltante:**
- Configuración de Polly retry policy
- Wrapper de descarga con retry

**Ubicación:**
- `YoutubeRag.Infrastructure/Services/VideoDownloadService.cs` (nuevo)

**Prioridad:** P2 (Medium - puede ser parte de YRUS-0302 Retry Logic epic)

**Esfuerzo:** 3 horas (si se implementa ahora)

**Recomendación:** Dejar para YRUS-0302 (Epic 4 - Background Job Orchestration) para no duplicar esfuerzo.

**Implementación sugerida (si se hace ahora):**

```csharp
using Polly;
using Polly.Retry;

public class VideoDownloadService : IVideoDownloadService
{
    private readonly AsyncRetryPolicy _networkRetryPolicy;

    public VideoDownloadService(...)
    {
        // Configure retry policy
        _networkRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * 5), // 10s, 20s, 40s
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Network error during download. Retry {RetryCount} after {Delay}s. Error: {Error}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });
    }

    public async Task<string> DownloadVideoAsync(...)
    {
        return await _networkRetryPolicy.ExecuteAsync(async () =>
        {
            // Download logic here
            return await DownloadVideoInternalAsync(...);
        });
    }
}
```

---

### GAP 8: Estructura de Directorios NO Coincide con AC3

**AC afectado:** AC3 - Gestión de Archivos Temporales

**Descripción:**
El servicio actual almacena archivos en `data/audio` (línea 29) en lugar de la estructura especificada:
- **Requerido:** `{TempPath}/{videoId}/{timestamp}.mp4` y `{TempPath}/{videoId}/{timestamp}.wav`
- **Actual:** `data/audio/{youTubeId}_audio_{timestamp}.mp3`

**Código faltante:**
- Creación de subdirectorio por video
- Naming con timestamp incluido

**Ubicación:**
- Modificar: `AudioExtractionService` para usar `ITempFileManagementService`
- Implementar: `TempFileManagementService.CreateVideoDirectory()`

**Prioridad:** P1 (Alto - organización importante)

**Esfuerzo:** 1 hora

**Implementación sugerida:**

```csharp
// En VideoDownloadService
public async Task<string> DownloadVideoAsync(string youTubeId, ...)
{
    // Create video-specific directory
    var videoDir = _tempFileService.CreateVideoDirectory(youTubeId);

    // Generate file path with timestamp
    var filePath = _tempFileService.GenerateFilePath(youTubeId, ".mp4");
    // Result: {TempPath}/abc123/abc123_20251009143022.mp4

    // Download to this path...
}

// En AudioExtractionService
public async Task<string> ExtractWhisperAudioFromVideoAsync(
    string videoFilePath,
    string youTubeId,
    CancellationToken cancellationToken)
{
    // Generate audio path in same video directory
    var audioPath = _tempFileService.GenerateFilePath(youTubeId, ".wav");
    // Result: {TempPath}/abc123/abc123_20251009143045.wav

    // Extract audio to this path...
}
```

**Estructura resultante:**
```
C:\Temp\YoutubeRag\
├── abc123\
│   ├── abc123_20251009143022.mp4 (video - eliminado después)
│   └── abc123_20251009143045.wav (audio - eliminado después de transcripción)
├── def456\
│   └── def456_20251009144030.wav
└── ...
```

---

### GAP 9: NO Maneja Casos Edge de AC5

**AC afectado:** AC5 - Manejo de Errores y Retry

**Descripción:**
Faltan manejadores específicos para:
- YouTube rate limit con espera y retry
- FFmpeg errors con logging detallado
- Video privado/eliminado con error descriptivo
- Geo-restricted videos

**Código faltante:**
- Excepciones custom para cada caso
- Manejo específico en catch blocks
- Mensajes de error user-friendly

**Ubicación:**
- Modificar: `VideoDownloadService` y `AudioExtractionService`
- Crear: Excepciones custom en `YoutubeRag.Domain/Exceptions/`

**Prioridad:** P2 (Medium)

**Esfuerzo:** 2 horas

**Implementación sugerida:**

```csharp
// Domain/Exceptions/YouTubeExceptions.cs - CREAR

public class YouTubeVideoUnavailableException : Exception
{
    public string YouTubeId { get; }
    public VideoUnavailableReason Reason { get; }

    public YouTubeVideoUnavailableException(
        string youTubeId,
        VideoUnavailableReason reason,
        string message)
        : base(message)
    {
        YouTubeId = youTubeId;
        Reason = reason;
    }
}

public enum VideoUnavailableReason
{
    Private,
    Deleted,
    GeoRestricted,
    AgeRestricted,
    LiveStream,
    Unknown
}

public class YouTubeRateLimitException : Exception
{
    public TimeSpan RetryAfter { get; }

    public YouTubeRateLimitException(TimeSpan retryAfter, string message)
        : base(message)
    {
        RetryAfter = retryAfter;
    }
}

public class InsufficientDiskSpaceException : Exception
{
    public long RequiredBytes { get; }
    public long AvailableBytes { get; }

    public InsufficientDiskSpaceException(
        long requiredBytes,
        long availableBytes,
        string message)
        : base(message)
    {
        RequiredBytes = requiredBytes;
        AvailableBytes = availableBytes;
    }
}

public class FFmpegException : Exception
{
    public int ExitCode { get; }
    public string StandardError { get; }

    public FFmpegException(int exitCode, string stderr, string message)
        : base(message)
    {
        ExitCode = exitCode;
        StandardError = stderr;
    }
}
```

**Uso en VideoDownloadService:**

```csharp
public async Task<string> DownloadVideoAsync(string youTubeId, ...)
{
    try
    {
        var video = await _youtubeClient.Videos.GetAsync(youTubeId);

        // Check if video is available
        if (video == null)
        {
            throw new YouTubeVideoUnavailableException(
                youTubeId,
                VideoUnavailableReason.Deleted,
                $"Video not found or has been deleted: {youTubeId}");
        }

        // Download...
    }
    catch (VideoUnplayableException ex)
    {
        // YouTube video is unplayable (private, deleted, etc.)
        var reason = DetermineUnavailableReason(ex);
        throw new YouTubeVideoUnavailableException(
            youTubeId,
            reason,
            GetUserFriendlyMessage(reason, youTubeId));
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    {
        // Rate limited by YouTube
        var retryAfter = TimeSpan.FromMinutes(5);
        throw new YouTubeRateLimitException(
            retryAfter,
            $"YouTube rate limit exceeded. Retry after {retryAfter.TotalMinutes} minutes");
    }
}

private string GetUserFriendlyMessage(VideoUnavailableReason reason, string youTubeId)
{
    return reason switch
    {
        VideoUnavailableReason.Private =>
            $"Video {youTubeId} is private and cannot be downloaded",
        VideoUnavailableReason.Deleted =>
            $"Video {youTubeId} has been deleted or removed",
        VideoUnavailableReason.GeoRestricted =>
            $"Video {youTubeId} is not available in your region",
        VideoUnavailableReason.AgeRestricted =>
            $"Video {youTubeId} is age-restricted and requires authentication",
        _ =>
            $"Video {youTubeId} is unavailable for download"
    };
}
```

---

## Resumen de Implementación

### Cobertura Actual vs AC

| AC | Descripción | Implementado | Gaps |
|----|-------------|--------------|------|
| **AC1** | Descarga de Video con Streaming | **30%** | GAP 1, 3, 4 |
| **AC2** | Extracción de Audio con FFmpeg | **40%** | GAP 2, 3, 8 |
| **AC3** | Gestión de Archivos Temporales | **50%** | GAP 5, 6, 8 |
| **AC4** | Tracking de Progreso | **20%** | GAP 4 |
| **AC5** | Manejo de Errores y Retry | **40%** | GAP 7, 9 |

**Implementación general:** ~45% completo

### Gaps por Prioridad

**P0 (Crítico - Bloqueante):**
1. GAP 1: Descarga de Video Intermedia - 4 horas
2. GAP 2: Normalización Audio para Whisper - 3 horas

**Subtotal P0:** 7 horas

**P1 (Alto - Importante):**
3. GAP 3: Actualización de VideoStatus - 2 horas
4. GAP 4: Progress Tracking - 3 horas
5. GAP 5: Verificación de Espacio en Disco - 2 horas
6. GAP 6: Hangfire Cleanup Job - 2 horas
7. GAP 8: Estructura de Directorios - 1 hora

**Subtotal P1:** 10 horas

**P2 (Medium - Nice to have):**
8. GAP 7: Retry Logic (recomendado para YRUS-0302) - 3 horas
9. GAP 9: Manejo de Casos Edge - 2 horas

**Subtotal P2:** 5 horas

**Esfuerzo total:** 22 horas (~2.75 días)

**Esfuerzo mínimo viable (solo P0):** 7 horas (~1 día)

---

## Integración Actual con Jobs

### Cómo se usa actualmente AudioExtractionService

```
VideosController.UploadYouTubeAsync()
    └─> VideoProcessingService.ProcessVideoAsync()
        └─> BackgroundJobService.EnqueueTranscriptionJob()
            └─> Hangfire: TranscriptionBackgroundJob.ExecuteAsync()
                └─> TranscriptionJobProcessor.ProcessTranscriptionJobAsync()
                    ├─> AudioExtractionService.ExtractAudioFromYouTubeAsync()
                    ├─> TranscriptionService.TranscribeAudioAsync()
                    └─> SegmentationService.CreateSegmentsFromTranscriptAsync()
```

**PROBLEMA:** No hay etapa separada de "Download Video" antes de "Extract Audio".

### Flujo requerido por AC

```
VideosController.UploadYouTubeAsync()
    └─> VideoProcessingService.ProcessVideoAsync()
        └─> BackgroundJobService.EnqueueTranscriptionJob()
            └─> Hangfire: TranscriptionBackgroundJob.ExecuteAsync()
                └─> TranscriptionJobProcessor.ProcessTranscriptionJobAsync()
                    ├─> [NUEVO] VideoDownloadService.DownloadVideoAsync()
                    │   └─> Update VideoStatus = Downloading
                    │   └─> Report progress every 10s
                    │   └─> Return video.mp4 path
                    ├─> [MODIFICAR] AudioExtractionService.ExtractWhisperAudioFromVideoAsync(video.mp4)
                    │   └─> FFmpeg conversion to 16kHz mono WAV
                    │   └─> Update VideoStatus = AudioExtracted
                    │   └─> Delete video.mp4
                    ├─> TranscriptionService.TranscribeAudioAsync()
                    └─> SegmentationService.CreateSegmentsFromTranscriptAsync()
```

---

## Decisiones de Arquitectura Requeridas

### Decisión 1: Enfoque de Descarga

**Opción A (Recomendada - Cumple AC):**
- Descargar video completo MP4
- Extraer audio con FFmpeg
- Eliminar MP4

**Pros:**
- Cumple AC literalmente
- Control total sobre formato de salida
- Consistencia con FFmpeg para normalización
- Permite reutilizar video en futuro si se necesita

**Cons:**
- Descarga y procesa más datos (~2x tamaño audio)
- Mayor uso de disco temporal

**Opción B (Actual):**
- Descargar solo audio directamente
- Convertir a formato requerido

**Pros:**
- Más eficiente (menos datos)
- Más rápido

**Cons:**
- NO cumple AC1 y AC2 estrictamente
- Menos control sobre formato

**RECOMENDACIÓN:** Opción A para MVP (cumplir AC), optimizar después si se necesita.

### Decisión 2: ¿Cuándo implementar Retry Logic?

**Opción A:** Implementar ahora (GAP 7)
- Esfuerzo: +3 horas
- Beneficio: Epic 3 100% completo

**Opción B (Recomendada):** Dejar para YRUS-0302
- Esfuerzo: 0 horas ahora
- Beneficio: No duplicar esfuerzo, ya planificado en Epic 4

**RECOMENDACIÓN:** Opción B - Implementar solo retry básico de Hangfire (ya configurado en línea 40 de TranscriptionBackgroundJob).

### Decisión 3: Implementación de TempFileManagementService

**CRÍTICO:** El archivo existe pero está VACÍO (solo 1 línea).

**Acción requerida:** Implementar completamente el servicio según la interfaz definida.

**Esfuerzo:** 3 horas adicionales (incluido en GAP 5 y 6)

---

## Recomendación de Next Steps

### Opción A: Implementación Completa

**Alcance:** Implementar todos los gaps P0 + P1
**Esfuerzo:** 17 horas (~2 días)
**Resultado:** Epic 3 95% completo (falta solo P2)

**Secuencia:**
1. Implementar TempFileManagementService (3h)
2. Implementar VideoDownloadService (GAP 1) (4h)
3. Modificar AudioExtractionService para Whisper (GAP 2) (3h)
4. Agregar actualización de VideoStatus (GAP 3) (2h)
5. Implementar progress tracking (GAP 4) (3h)
6. Crear TempFileCleanupJob (GAP 6) (2h)

### Opción B: MVP Mínimo

**Alcance:** Implementar solo gaps P0 críticos
**Esfuerzo:** 7 horas (~1 día)
**Resultado:** Epic 3 60% completo (funcional pero sin tracking/cleanup)

**Secuencia:**
1. Implementar VideoDownloadService básico (GAP 1) (4h)
2. Modificar AudioExtractionService para Whisper (GAP 2) (3h)

**Gaps diferidos:**
- GAP 3, 4 → Sprint 3
- GAP 5, 6 → Sprint 3
- GAP 7 → YRUS-0302
- GAP 8, 9 → Sprint 3

### Opción C (Recomendada): MVP + Infraestructura

**Alcance:** P0 + infraestructura base (TempFileService + Cleanup)
**Esfuerzo:** 12 horas (~1.5 días)
**Resultado:** Epic 3 75% completo (funcional + base sólida)

**Secuencia:**
1. Implementar TempFileManagementService completo (3h)
2. Implementar VideoDownloadService con disk check (GAP 1 + GAP 5) (5h)
3. Modificar AudioExtractionService para Whisper (GAP 2) (3h)
4. Crear TempFileCleanupJob (GAP 6) (1h)

**Gaps diferidos:**
- GAP 3, 4 → Implementar en YRUS-0401 (Progress Tracking epic)
- GAP 7 → YRUS-0302 (Retry Logic epic)
- GAP 8, 9 → Sprint 3

---

## Impacto en Otros Componentes

### Archivos que requieren modificación

1. **TranscriptionJobProcessor.cs** (línea 111-128)
   - Agregar paso de descarga de video
   - Modificar llamada a audio extraction
   - Agregar progress tracking

2. **RecurringJobsSetup.cs**
   - Agregar TempFileCleanupJob

3. **Program.cs** (DI registration)
   - Registrar VideoDownloadService
   - Registrar TempFileManagementService

4. **appsettings.json**
   - Agregar configuración de TempFilePath
   - Agregar CleanupAfterHours

### Archivos nuevos requeridos

1. `YoutubeRag.Infrastructure/Services/VideoDownloadService.cs`
2. `YoutubeRag.Infrastructure/Services/TempFileManagementService.cs` (IMPLEMENTAR - ya existe vacío)
3. `YoutubeRag.Infrastructure/Jobs/TempFileCleanupJob.cs`
4. `YoutubeRag.Domain/Exceptions/YouTubeExceptions.cs` (opcional P2)

### Riesgo de regresión

**BAJO:** Los cambios son principalmente aditivos.
- AudioExtractionService actual puede mantenerse como fallback
- VideoDownloadService es nuevo
- TranscriptionJobProcessor cambios son localizados

**Testing requerido:**
- Integration tests para nuevo flujo
- Regression tests de transcripción existente
- Manual testing con video real

---

## Conclusión

El `AudioExtractionService` tiene una **implementación funcional** que permite transcribir videos de YouTube, pero **NO cumple los AC específicos de YRUS-0103** que requieren:

1. Descarga de video MP4 separada
2. Extracción FFmpeg con normalización Whisper (16kHz mono WAV)
3. Progress tracking granular
4. Gestión robusta de archivos temporales
5. Cleanup automático

**Recomendación:** Implementar **Opción C (MVP + Infraestructura)** con esfuerzo de **12 horas** para tener una base sólida, diferir progress tracking detallado a Epic 6 (YRUS-0401) y retry logic a Epic 4 (YRUS-0302).

Esto permite **avanzar con Sprint 2** sin bloquear otras historias, mientras se completa la funcionalidad core de Epic 3.

---

**Validación completada:** 9 de Octubre, 2025
**Próxima acción:** Decisión de Product Owner sobre opción de implementación
**Tiempo estimado de implementación:** 12-17 horas según opción elegida
