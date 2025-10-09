using Microsoft.Extensions.Options;
using YoutubeRag.Application.Configuration;

namespace YoutubeRag.Api.Configuration;

/// <summary>
/// Adapter to provide AppSettings as IAppConfiguration to the Application layer
/// </summary>
public class AppConfigurationAdapter : IAppConfiguration
{
    private readonly AppSettings _appSettings;

    public AppConfigurationAdapter(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public string Environment => _appSettings.Environment;
    public string AudioStoragePath => _appSettings.AudioStoragePath;
    public string WhisperModelSize => _appSettings.WhisperModelSize;
    public bool AutoTranscribe => _appSettings.AutoTranscribe;
    public int MaxAudioFileSizeMB => _appSettings.MaxAudioFileSizeMB;
    public int EmbeddingDimension => _appSettings.EmbeddingDimension;
    public int EmbeddingBatchSize => _appSettings.EmbeddingBatchSize;
    public bool AutoGenerateEmbeddings => _appSettings.AutoGenerateEmbeddings;
    public int MaxSegmentLength => _appSettings.MaxSegmentLength;
    public int MinSegmentLength => _appSettings.MinSegmentLength;
    public bool EnableAutoModelDowngrade => _appSettings.EnableAutoModelDowngrade;
    public string? TempFilePath => _appSettings.TempFilePath;
    public int? CleanupAfterHours => _appSettings.CleanupAfterHours;
    public int? MinDiskSpaceGB => _appSettings.MinDiskSpaceGB;
}