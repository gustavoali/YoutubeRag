using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace YoutubeRag.Infrastructure.Services;

public class MockTranscriptionService : ITranscriptionService
{
    private readonly ILogger<MockTranscriptionService> _logger;

    public MockTranscriptionService(ILogger<MockTranscriptionService> logger)
    {
        _logger = logger;
    }

    public async Task<TranscriptionResult> TranscribeAudioAsync(string audioFilePath, string language = "auto")
    {
        _logger.LogInformation("Mock: Transcribing audio file: {FilePath}", audioFilePath);

        await Task.Delay(3000); // Simulate transcription time

        var mockSegments = new List<TranscriptionSegment>
        {
            new TranscriptionSegment
            {
                Index = 0,
                StartTime = 0.0,
                EndTime = 5.0,
                Text = "Welcome to this mock YouTube video transcription.",
                Confidence = 0.95,
                Words = new List<Word>
                {
                    new Word { Text = "Welcome", StartTime = 0.0, EndTime = 1.0, Confidence = 0.98 },
                    new Word { Text = "to", StartTime = 1.0, EndTime = 1.2, Confidence = 0.99 },
                    new Word { Text = "this", StartTime = 1.2, EndTime = 1.5, Confidence = 0.97 },
                    new Word { Text = "mock", StartTime = 1.5, EndTime = 2.0, Confidence = 0.92 },
                    new Word { Text = "YouTube", StartTime = 2.0, EndTime = 2.8, Confidence = 0.99 },
                    new Word { Text = "video", StartTime = 2.8, EndTime = 3.3, Confidence = 0.96 },
                    new Word { Text = "transcription", StartTime = 3.3, EndTime = 5.0, Confidence = 0.94 }
                }
            },
            new TranscriptionSegment
            {
                Index = 1,
                StartTime = 5.0,
                EndTime = 12.0,
                Text = "Today we will be exploring the fascinating world of artificial intelligence and machine learning.",
                Confidence = 0.93,
                Words = new List<Word>()
            },
            new TranscriptionSegment
            {
                Index = 2,
                StartTime = 12.0,
                EndTime = 20.0,
                Text = "This technology has revolutionized how we process information and understand natural language.",
                Confidence = 0.91,
                Words = new List<Word>()
            },
            new TranscriptionSegment
            {
                Index = 3,
                StartTime = 20.0,
                EndTime = 28.0,
                Text = "Thank you for watching this demonstration of our YouTube RAG system.",
                Confidence = 0.96,
                Words = new List<Word>()
            }
        };

        return new TranscriptionResult
        {
            Text = string.Join(" ", mockSegments.Select(s => s.Text)),
            Language = language == "auto" ? "en" : language,
            Confidence = 0.94,
            Duration = TimeSpan.FromSeconds(28),
            Segments = mockSegments
        };
    }

    public async Task<TranscriptionResult> TranscribeAudioFromUrlAsync(string audioUrl, string language = "auto")
    {
        _logger.LogInformation("Mock: Transcribing audio from URL: {Url}", audioUrl);

        // Simulate downloading and transcribing
        await Task.Delay(4000);

        return await TranscribeAudioAsync("mock_audio_from_url.mp3", language);
    }

    public async Task<List<TranscriptSegment>> ProcessTranscriptionAsync(string videoId, TranscriptionResult transcription)
    {
        _logger.LogInformation("Mock: Processing transcription for video: {VideoId}", videoId);

        await Task.Delay(500);

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
        await Task.Delay(50);

        var supportedLanguages = await GetSupportedLanguagesAsync();
        return supportedLanguages.Contains(language.ToLowerInvariant());
    }

    public async Task<List<string>> GetSupportedLanguagesAsync()
    {
        await Task.Delay(50);

        return new List<string>
        {
            "auto", "en", "es", "fr", "de", "it", "pt", "ru", "ja", "ko", "zh", "ar", "hi", "tr", "pl", "nl", "sv", "da", "no", "fi"
        };
    }
}