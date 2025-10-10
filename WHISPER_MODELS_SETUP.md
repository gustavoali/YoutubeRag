# Whisper Models Management - Setup Guide

## Overview

The YouTube RAG .NET application includes automatic management of Whisper AI models for audio transcription. Models are downloaded automatically when needed and cleaned up periodically.

## Model Selection Strategy

The system automatically selects the optimal model based on video duration:

| Video Duration | Model Selected | Model Size | Processing Speed |
|---------------|----------------|------------|------------------|
| < 10 minutes  | tiny           | ~39 MB     | ~10x realtime    |
| 10-30 minutes | base           | ~74 MB     | ~7x realtime     |
| > 30 minutes  | small          | ~244 MB    | ~4x realtime     |

## Configuration

All Whisper model settings are configured in `appsettings.json`:

```json
{
  "Whisper": {
    "ModelsPath": "C:\\Models\\Whisper",
    "DefaultModel": "auto",
    "ForceModel": null,
    "ModelDownloadUrl": "https://openaipublic.azureedge.net/main/whisper/models/",
    "CleanupUnusedModelsDays": 30,
    "MinDiskSpaceGB": 10,
    "DownloadRetryAttempts": 3,
    "DownloadRetryDelaySeconds": 5,
    "ModelCacheDurationMinutes": 60,
    "TinyModelThresholdSeconds": 600,
    "BaseModelThresholdSeconds": 1800
  }
}
```

### Configuration Options

- **ModelsPath**: Directory where models are stored (default: `C:\Models\Whisper`)
- **DefaultModel**: Set to `"auto"` for automatic selection or specify a model name
- **ForceModel**: Override automatic selection with a specific model (`null` to disable)
- **ModelDownloadUrl**: CDN URL for downloading models
- **CleanupUnusedModelsDays**: Delete models not used for this many days (default: 30)
- **MinDiskSpaceGB**: Minimum required disk space before downloads (default: 10 GB)
- **DownloadRetryAttempts**: Number of retry attempts for failed downloads (default: 3)
- **DownloadRetryDelaySeconds**: Delay between retries in seconds (default: 5)
- **ModelCacheDurationMinutes**: Cache duration for model availability list (default: 60)
- **TinyModelThresholdSeconds**: Duration threshold for tiny model selection (default: 600 = 10 min)
- **BaseModelThresholdSeconds**: Duration threshold for base model selection (default: 1800 = 30 min)

## Supported Models (MVP)

The following models are supported in the MVP release:

1. **tiny** - 39 MB
   - Fastest processing
   - Good for short videos
   - Lower accuracy

2. **base** - 74 MB
   - Balanced speed and accuracy
   - Good for medium-length videos

3. **small** - 244 MB
   - Higher accuracy
   - Good for longer videos
   - Slower processing

**Note**: The `medium` (769 MB) and `large` (1550 MB) models are NOT supported in the MVP to keep resource requirements reasonable.

## Automatic Model Management

### Model Detection (AC1)

The system automatically:
- Scans the models directory on startup
- Lists available models (tiny, base, small)
- Validates model file integrity
- Caches the list to avoid repeated disk scans

### Model Download (AC2)

When a model is needed but not available:
1. Verifies sufficient disk space (requires 2x model size free)
2. Downloads from OpenAI CDN
3. Shows progress in logs (every 10%)
4. Computes SHA256 checksum for validation
5. Retries up to 3 times on failure
6. Updates job status to Failed if download fails

### Model Cleanup (AC4)

A Hangfire recurring job runs daily at 3:00 AM to:
- Delete models not used for >30 days (configurable)
- ALWAYS keeps the `tiny` model as a fallback
- Logs cleanup actions
- Only runs if configured cleanup period has elapsed

## Manual Setup (Optional)

While models download automatically, you can pre-download them:

1. Create the models directory:
   ```bash
   mkdir C:\Models\Whisper
   ```

2. Download models manually:
   - tiny: https://openaipublic.azureedge.net/main/whisper/models/tiny.pt
   - base: https://openaipublic.azureedge.net/main/whisper/models/base.pt
   - small: https://openaipublic.azureedge.net/main/whisper/models/small.pt

3. Place models in subdirectories:
   ```
   C:\Models\Whisper\
   ├── tiny\
   │   └── tiny.pt
   ├── base\
   │   └── base.pt
   └── small\
       └── small.pt
   ```

## Disk Space Requirements

Ensure sufficient disk space:

- **Minimum**: 10 GB free (configurable)
- **Recommended**: 20 GB free for comfortable operation
- **Per model download**: 2x model size required

The system will:
- Check disk space before downloads
- Log warnings if space < 10 GB
- Fail jobs if insufficient space

## Error Handling (AC5)

The system handles common errors:

| Error | Action |
|-------|--------|
| Download failure | Retry up to 3 times with 5s delay |
| Checksum mismatch | Delete corrupted file and retry |
| Insufficient disk space | Fail job with clear error message |
| Corrupted model | Delete and re-download |
| Network timeout | Retry with exponential backoff |

## Monitoring

### Logs

Model operations are logged with structured logging:

```
[12:34:56 INF] YoutubeRag.Application.Services.WhisperModelManager
Auto-selected model: base for video duration: 1200s (20.0 min)

[12:35:00 INF] YoutubeRag.Infrastructure.Services.WhisperModelDownloadService
Downloading from https://openaipublic.azureedge.net/main/whisper/models/base.pt

[12:35:15 INF] YoutubeRag.Infrastructure.Services.WhisperModelDownloadService
Downloading base: 50.0% (37.0 MB / 74.0 MB)

[12:35:30 INF] YoutubeRag.Infrastructure.Services.WhisperModelDownloadService
Download complete for base: 74.0 MB

[12:35:31 INF] YoutubeRag.Infrastructure.Services.WhisperModelDownloadService
Model base validated - Size: 74.0 MB, Checksum: abc123...
```

### Health Checks

The `/health` endpoint includes Whisper model status.

## Testing

The implementation includes comprehensive tests:

- **53 unit tests** covering:
  - Model selection logic (13 tests)
  - Checksum validation (5 tests)
  - Download operations (8 tests)
  - Cleanup operations (3 tests)
  - Error handling (11 tests)
  - Edge cases (13 tests)

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~WhisperModel"
```

## API Integration

To use model management in your code:

```csharp
// Inject the service
private readonly IWhisperModelService _modelService;

// Auto-select model based on duration
var modelName = await _modelService.SelectModelForDurationAsync(videoDurationSeconds);

// Get model path (downloads if needed)
var modelPath = await _modelService.GetModelPathAsync(modelName);

// Check model availability
var isAvailable = await _modelService.IsModelAvailableAsync("tiny");

// Get available models
var models = await _modelService.GetAvailableModelsAsync();

// Get model metadata
var metadata = await _modelService.GetModelMetadataAsync("base");
```

## Troubleshooting

### Model not downloading

1. Check disk space: `df -h` or `Get-PSDrive`
2. Verify network connectivity to CDN
3. Check logs for error messages
4. Verify configuration in appsettings.json

### Download fails repeatedly

1. Check CDN availability
2. Increase retry attempts in configuration
3. Verify firewall/proxy settings
4. Check available disk space

### Models taking too much space

1. Reduce `CleanupUnusedModelsDays` to cleanup more frequently
2. Delete unused models manually
3. Force use of smaller models with `ForceModel` setting

## Production Recommendations

1. **Pre-download models** in production deployments to avoid first-run delays
2. **Monitor disk space** with alerts at 80% capacity
3. **Use network storage** for models in containerized environments
4. **Configure cleanup** based on usage patterns
5. **Set ForceModel** if you know your typical video duration range

## Security Considerations

- Models are downloaded from official OpenAI CDN
- SHA256 checksums are computed and logged
- No arbitrary code execution from model files
- Download URLs are configurable but default to official source

## Future Enhancements

Planned improvements (not in MVP):

- Support for medium/large models
- Custom model URLs
- Model sharing across instances
- Compressed model storage
- Download bandwidth throttling
- Parallel downloads
