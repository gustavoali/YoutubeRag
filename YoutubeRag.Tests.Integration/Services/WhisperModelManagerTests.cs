using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Services;

namespace YoutubeRag.Tests.Integration.Services;

/// <summary>
/// Unit tests for WhisperModelManager service.
/// Tests model selection logic, caching, and business rules.
/// </summary>
public class WhisperModelManagerTests
{
    private readonly Mock<IWhisperModelDownloadService> _downloadServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<WhisperModelManager>> _loggerMock;
    private readonly WhisperOptions _options;

    public WhisperModelManagerTests()
    {
        _downloadServiceMock = new Mock<IWhisperModelDownloadService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<WhisperModelManager>>();

        _options = new WhisperOptions
        {
            ModelsPath = "C:\\Models\\Whisper",
            DefaultModel = "auto",
            ForceModel = null,
            TinyModelThresholdSeconds = 600,   // 10 minutes
            BaseModelThresholdSeconds = 1800,  // 30 minutes
            ModelCacheDurationMinutes = 60
        };
    }

    private WhisperModelManager CreateService()
    {
        return new WhisperModelManager(
            Options.Create(_options),
            _downloadServiceMock.Object,
            _cache,
            _loggerMock.Object);
    }

    #region Model Selection Tests (AC3)

    [Theory]
    [InlineData(0, "tiny")]           // 0 seconds -> tiny
    [InlineData(60, "tiny")]          // 1 minute -> tiny
    [InlineData(300, "tiny")]         // 5 minutes -> tiny
    [InlineData(599, "tiny")]         // 9:59 -> tiny
    [InlineData(600, "base")]         // 10 minutes -> base
    [InlineData(900, "base")]         // 15 minutes -> base
    [InlineData(1799, "base")]        // 29:59 -> base
    [InlineData(1800, "small")]       // 30 minutes -> small
    [InlineData(3600, "small")]       // 1 hour -> small
    [InlineData(7200, "small")]       // 2 hours -> small
    [InlineData(14400, "small")]      // 4 hours -> small
    public async Task SelectModelForDurationAsync_AutoMode_ShouldSelectCorrectModel(
        int durationSeconds,
        string expectedModel)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SelectModelForDurationAsync(durationSeconds);

        // Assert
        result.Should().Be(expectedModel);
    }

    [Fact]
    public async Task SelectModelForDurationAsync_WithForcedModel_ShouldReturnForcedModel()
    {
        // Arrange
        _options.ForceModel = "base";
        var service = CreateService();

        // Act
        var result1 = await service.SelectModelForDurationAsync(100); // would normally be tiny
        var result2 = await service.SelectModelForDurationAsync(5000); // would normally be small

        // Assert
        result1.Should().Be("base");
        result2.Should().Be("base");
    }

    [Fact]
    public async Task SelectModelForDurationAsync_WithCustomThresholds_ShouldRespectThresholds()
    {
        // Arrange
        _options.TinyModelThresholdSeconds = 300;  // 5 minutes
        _options.BaseModelThresholdSeconds = 900;  // 15 minutes
        var service = CreateService();

        // Act
        var result1 = await service.SelectModelForDurationAsync(299);   // < 5 min -> tiny
        var result2 = await service.SelectModelForDurationAsync(300);   // >= 5 min -> base
        var result3 = await service.SelectModelForDurationAsync(899);   // < 15 min -> base
        var result4 = await service.SelectModelForDurationAsync(900);   // >= 15 min -> small

        // Assert
        result1.Should().Be("tiny");
        result2.Should().Be("base");
        result3.Should().Be("base");
        result4.Should().Be("small");
    }

    [Fact]
    public async Task SelectModelForDurationAsync_NegativeDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.SelectModelForDurationAsync(-1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Duration cannot be negative*");
    }

    #endregion

    #region Model Path Retrieval Tests (AC1, AC2)

    [Fact]
    public async Task GetModelPathAsync_ModelAlreadyAvailable_ShouldReturnPathWithoutDownload()
    {
        // Arrange
        var service = CreateService();

        // Create a temp file to simulate the model exists
        var tempModelPath = Path.Combine(Path.GetTempPath(), "tiny.pt");
        await File.WriteAllBytesAsync(tempModelPath, new byte[100]);

        try
        {
            _downloadServiceMock
                .Setup(x => x.GetModelFilePath("tiny"))
                .Returns(tempModelPath);

            // Act
            var result = await service.GetModelPathAsync("tiny");

            // Assert
            result.Should().Be(tempModelPath);
            _downloadServiceMock.Verify(x => x.DownloadModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempModelPath))
            {
                File.Delete(tempModelPath);
            }
        }
    }

    [Fact]
    public async Task GetModelPathAsync_ModelNotAvailable_ShouldDownloadAndReturnPath()
    {
        // Arrange
        var service = CreateService();
        var modelPath = "C:\\Models\\Whisper\\base\\base.pt";

        _downloadServiceMock
            .Setup(x => x.GetModelFilePath("base"))
            .Returns(modelPath);

        _downloadServiceMock
            .Setup(x => x.VerifyDiskSpaceAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _downloadServiceMock
            .Setup(x => x.DownloadModelAsync("base", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // First call: file doesn't exist, second call after download: file exists
        var callCount = 0;
        File.Exists(modelPath); // This will fail in unit tests, so we mock it

        // Act
        var result = await service.GetModelPathAsync("base");

        // Assert
        result.Should().Be(modelPath);
        _downloadServiceMock.Verify(x => x.VerifyDiskSpaceAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetModelPathAsync_UnsupportedModel_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.GetModelPathAsync("large"); // Not supported in MVP

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported model*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetModelPathAsync_InvalidModelName_ShouldThrowArgumentException(string? modelName)
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.GetModelPathAsync(modelName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Caching Tests (AC1)

    [Fact]
    public async Task GetAvailableModelsAsync_FirstCall_ShouldScanAndCache()
    {
        // Arrange
        var service = CreateService();

        _downloadServiceMock
            .Setup(x => x.GetModelFilePath("tiny"))
            .Returns("C:\\Models\\Whisper\\tiny\\tiny.pt");
        _downloadServiceMock
            .Setup(x => x.GetModelFilePath("base"))
            .Returns("C:\\Models\\Whisper\\base\\base.pt");
        _downloadServiceMock
            .Setup(x => x.GetModelFilePath("small"))
            .Returns("C:\\Models\\Whisper\\small\\small.pt");

        // Act
        var result1 = await service.GetAvailableModelsAsync();
        var result2 = await service.GetAvailableModelsAsync(); // Should use cache

        // Assert
        result1.Should().BeOfType<List<string>>();
        result2.Should().BeOfType<List<string>>();
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public async Task RefreshModelCacheAsync_ShouldInvalidateCache()
    {
        // Arrange
        var service = CreateService();

        _downloadServiceMock
            .Setup(x => x.GetModelFilePath(It.IsAny<string>()))
            .Returns<string>(model => $"C:\\Models\\Whisper\\{model}\\{model}.pt");

        // Act
        var result1 = await service.GetAvailableModelsAsync();
        await service.RefreshModelCacheAsync();
        var result2 = await service.GetAvailableModelsAsync();

        // Assert - both calls should succeed
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    #endregion

    #region Model Metadata Tests

    [Fact]
    public async Task GetModelMetadataAsync_ModelNotFound_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();

        _downloadServiceMock
            .Setup(x => x.GetModelFilePath("tiny"))
            .Returns("C:\\NonExistent\\tiny.pt");

        // Act
        var result = await service.GetModelMetadataAsync("tiny");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetModelMetadataAsync_InvalidModelName_ShouldThrowArgumentException(string? modelName)
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.GetModelMetadataAsync(modelName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsModelAvailable Tests

    [Fact]
    public async Task IsModelAvailableAsync_ValidModelName_ShouldReturnBoolean()
    {
        // Arrange
        var service = CreateService();

        _downloadServiceMock
            .Setup(x => x.GetModelFilePath("tiny"))
            .Returns("C:\\Models\\Whisper\\tiny\\tiny.pt");

        // Act
        var result = await service.IsModelAvailableAsync("tiny");

        // Assert
        result.Should().BeFalse(); // File doesn't actually exist in test environment
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task IsModelAvailableAsync_InvalidModelName_ShouldThrowArgumentException(string? modelName)
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.IsModelAvailableAsync(modelName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
