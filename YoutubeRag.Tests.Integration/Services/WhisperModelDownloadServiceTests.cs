using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Security.Cryptography;
using YoutubeRag.Application.Configuration;
using YoutubeRag.Infrastructure.Services;

namespace YoutubeRag.Tests.Integration.Services;

/// <summary>
/// Unit tests for WhisperModelDownloadService.
/// Tests download logic, checksum validation, and disk space management.
/// </summary>
public class WhisperModelDownloadServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<WhisperModelDownloadService>> _loggerMock;
    private readonly WhisperOptions _options;
    private readonly string _tempDirectory;

    public WhisperModelDownloadServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<WhisperModelDownloadService>>();

        // Use temp directory for tests
        _tempDirectory = Path.Combine(Path.GetTempPath(), "WhisperModelTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        _options = new WhisperOptions
        {
            ModelsPath = _tempDirectory,
            ModelDownloadUrl = "https://openaipublic.azureedge.net/main/whisper/models/",
            DownloadRetryAttempts = 3,
            DownloadRetryDelaySeconds = 1,
            MinDiskSpaceGB = 1 // Low threshold for tests
        };
    }

    private WhisperModelDownloadService CreateService()
    {
        return new WhisperModelDownloadService(
            Options.Create(_options),
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    #region GetModelFilePath Tests

    [Theory]
    [InlineData("tiny", "tiny.pt")]
    [InlineData("base", "base.pt")]
    [InlineData("small", "small.pt")]
    public void GetModelFilePath_ValidModel_ShouldReturnCorrectPath(string modelName, string expectedFileName)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetModelFilePath(modelName);

        // Assert
        result.Should().Contain(modelName);
        result.Should().EndWith(expectedFileName);
        result.Should().StartWith(_tempDirectory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetModelFilePath_InvalidModelName_ShouldThrowArgumentException(string? modelName)
    {
        // Arrange
        var service = CreateService();

        // Act
        Action act = () => service.GetModelFilePath(modelName!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Checksum Computation Tests (AC2)

    [Fact]
    public async Task ComputeChecksumAsync_ValidFile_ShouldReturnSHA256Hash()
    {
        // Arrange
        var service = CreateService();
        var testFilePath = Path.Combine(_tempDirectory, "test.txt");
        var testContent = "Hello, World!"u8.ToArray();

        await File.WriteAllBytesAsync(testFilePath, testContent);

        // Calculate expected hash
        using var sha256 = SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(testContent)).ToLowerInvariant();

        // Act
        var result = await service.ComputeChecksumAsync(testFilePath);

        // Assert
        result.Should().Be(expectedHash);
        result.Should().HaveLength(64); // SHA256 produces 64 hex characters
        result.Should().MatchRegex("^[a-f0-9]{64}$"); // Only lowercase hex
    }

    [Fact]
    public async Task ComputeChecksumAsync_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var service = CreateService();
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.txt");

        // Act
        Func<Task> act = async () => await service.ComputeChecksumAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ComputeChecksumAsync_InvalidFilePath_ShouldThrowArgumentException(string? filePath)
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.ComputeChecksumAsync(filePath!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ComputeChecksumAsync_EmptyFile_ShouldReturnValidHash()
    {
        // Arrange
        var service = CreateService();
        var emptyFilePath = Path.Combine(_tempDirectory, "empty.txt");

        await File.WriteAllBytesAsync(emptyFilePath, Array.Empty<byte>());

        // Act
        var result = await service.ComputeChecksumAsync(emptyFilePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveLength(64);
        // SHA256 of empty file is known
        result.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    #endregion

    #region Disk Space Verification Tests (AC4)

    [Fact]
    public async Task VerifyDiskSpaceAsync_SufficientSpace_ShouldComplete()
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.VerifyDiskSpaceAsync();

        // Assert - should not throw (assuming test machine has >1 GB free)
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task VerifyDiskSpaceAsync_InsufficientSpace_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _options.MinDiskSpaceGB = 999999; // Impossibly large requirement
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.VerifyDiskSpaceAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient disk space*");
    }

    #endregion

    #region Download Tests (AC2, AC5)

    [Fact]
    public async Task DownloadModelAsync_SuccessfulDownload_ShouldCreateFile()
    {
        // Arrange
        var service = CreateService();
        var modelContent = new byte[39_000_000]; // 39 MB to match tiny model size
        Array.Fill<byte>(modelContent, 0x42);

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(modelContent)
                };
                response.Content.Headers.ContentLength = modelContent.Length;
                return response;
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object)
            {
                Timeout = TimeSpan.FromMinutes(30)
            });

        // Act
        await service.DownloadModelAsync("tiny");

        // Assert
        var modelPath = service.GetModelFilePath("tiny");
        File.Exists(modelPath).Should().BeTrue();

        // Cleanup
        if (File.Exists(modelPath))
        {
            File.Delete(modelPath);
        }
    }

    [Fact]
    public async Task DownloadModelAsync_DownloadFailure_ShouldRetryAndThrow()
    {
        // Arrange
        var service = CreateService();

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object)
            {
                Timeout = TimeSpan.FromMinutes(30)
            });

        // Act
        Func<Task> act = async () => await service.DownloadModelAsync("tiny");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*after 3 attempts*");

        // Verify retries occurred
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(3), // Should retry 3 times
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DownloadModelAsync_InvalidModelName_ShouldThrowArgumentException(string? modelName)
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.DownloadModelAsync(modelName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DownloadModelAsync_UnknownModel_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.DownloadModelAsync("unknown-model");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unknown model*");
    }

    #endregion

    #region Cleanup Tests (AC4)

    [Fact]
    public async Task CleanupUnusedModelsAsync_NoModels_ShouldReturnZero()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CleanupUnusedModelsAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CleanupUnusedModelsAsync_OldModels_ShouldDeleteOnlyOldModels()
    {
        // Arrange
        var service = CreateService();

        // Create test model files with different ages
        var baseModelPath = service.GetModelFilePath("base");
        var smallModelPath = service.GetModelFilePath("small");

        Directory.CreateDirectory(Path.GetDirectoryName(baseModelPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(smallModelPath)!);

        await File.WriteAllBytesAsync(baseModelPath, new byte[100]);
        await File.WriteAllBytesAsync(smallModelPath, new byte[100]);

        // Set last access time to > 30 days ago for base model
        File.SetLastAccessTime(baseModelPath, DateTime.UtcNow.AddDays(-31));
        // Keep small model recent
        File.SetLastAccessTime(smallModelPath, DateTime.UtcNow);

        // Act
        var result = await service.CleanupUnusedModelsAsync();

        // Assert
        result.Should().Be(1); // Only base model should be deleted
        File.Exists(baseModelPath).Should().BeFalse();
        File.Exists(smallModelPath).Should().BeTrue();

        // Cleanup
        if (File.Exists(smallModelPath))
        {
            File.Delete(smallModelPath);
        }
    }

    [Fact]
    public async Task CleanupUnusedModelsAsync_TinyModel_ShouldNeverDelete()
    {
        // Arrange
        var service = CreateService();

        // Create old tiny model
        var tinyModelPath = service.GetModelFilePath("tiny");
        Directory.CreateDirectory(Path.GetDirectoryName(tinyModelPath)!);
        await File.WriteAllBytesAsync(tinyModelPath, new byte[100]);

        // Set last access time to > 30 days ago
        File.SetLastAccessTime(tinyModelPath, DateTime.UtcNow.AddDays(-365));

        // Act
        var result = await service.CleanupUnusedModelsAsync();

        // Assert
        result.Should().Be(0); // Tiny model should never be deleted
        File.Exists(tinyModelPath).Should().BeTrue();

        // Cleanup
        if (File.Exists(tinyModelPath))
        {
            File.Delete(tinyModelPath);
        }
    }

    #endregion

    // Cleanup after all tests
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
