using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace YoutubeRag.Infrastructure.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IYouTubeService _youTubeService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IJobService _jobService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _downloadPath;

    public VideoProcessingService(
        ILogger<VideoProcessingService> logger,
        ApplicationDbContext context,
        IYouTubeService youTubeService,
        ITranscriptionService transcriptionService,
        IEmbeddingService embeddingService,
        IJobService jobService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _context = context;
        _youTubeService = youTubeService;
        _transcriptionService = transcriptionService;
        _embeddingService = embeddingService;
        _jobService = jobService;
        _serviceProvider = serviceProvider;
        _downloadPath = Path.Combine(Path.GetTempPath(), "YoutubeRag", "Videos");
        Directory.CreateDirectory(_downloadPath);
    }

    public async Task<Video> ProcessVideoFromUrlAsync(string url, string title, string description, string userId)
    {
        Video video = null!;
        try
        {
            _logger.LogInformation("Starting video processing from URL: {Url}", url);
            _logger.LogInformation("Processing video for userId: '{UserId}'", userId);

            // Validate YouTube URL
            if (!await _youTubeService.IsValidYouTubeUrlAsync(url))
                throw new ArgumentException($"Invalid YouTube URL: {url}");

            // Get video info from YouTube
            var videoInfo = await _youTubeService.GetVideoInfoAsync(url);

            // Create video entity
            video = new Video
            {
                Id = Guid.NewGuid().ToString(),
                Title = title ?? videoInfo.Title,
                Description = description ?? videoInfo.Description,
                YouTubeId = videoInfo.Id,
                Url = url,
                OriginalUrl = url,
                ThumbnailUrl = videoInfo.ThumbnailUrl,
                Duration = videoInfo.Duration,
                ViewCount = videoInfo.ViewCount,
                LikeCount = videoInfo.LikeCount,
                Status = VideoStatus.Pending,
                UserId = userId,
                ProcessingProgress = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            // Create background job for processing
            var job = await _jobService.CreateJobAsync("ProcessVideoFromUrl", userId, video.Id, new Dictionary<string, object>
            {
                ["videoId"] = video.Id,
                ["url"] = url
            });

            _logger.LogInformation("Created video {VideoId} and job {JobId} for processing", video.Id, job.Id);

            // Start processing in background (in a real system, this would be handled by Hangfire)
            _ = Task.Run(async () => await ProcessVideoInBackgroundAsync(video.Id, url));

            return video;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video from URL: {Url}", url);

            if (video != null)
            {
                video.Status = VideoStatus.Failed;
                video.ErrorMessage = ex.Message;
                video.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            throw;
        }
    }

    public async Task<Video> ProcessVideoFromFileAsync(Stream fileStream, string fileName, string title, string description, string userId)
    {
        Video video = null!;
        try
        {
            _logger.LogInformation("Starting video processing from file: {FileName}", fileName);

            // Save uploaded file
            var videoId = Guid.NewGuid().ToString();
            var filePath = Path.Combine(_downloadPath, $"{videoId}_{fileName}");

            using (var fileStreamWriter = File.Create(filePath))
            {
                await fileStream.CopyToAsync(fileStreamWriter);
            }

            // Create video entity
            video = new Video
            {
                Id = videoId,
                Title = title ?? Path.GetFileNameWithoutExtension(fileName),
                Description = description ?? "",
                Status = VideoStatus.Pending,
                FilePath = filePath,
                UserId = userId,
                ProcessingProgress = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            // Create background job for processing
            var job = await _jobService.CreateJobAsync("ProcessVideoFromFile", userId, video.Id, new Dictionary<string, object>
            {
                ["videoId"] = video.Id,
                ["filePath"] = filePath
            });

            _logger.LogInformation("Created video {VideoId} and job {JobId} for file processing", video.Id, job.Id);

            // Start processing in background
            _ = Task.Run(async () => await ProcessVideoFileInBackgroundAsync(video.Id, filePath));

            return video;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video from file: {FileName}", fileName);

            if (video != null)
            {
                video.Status = VideoStatus.Failed;
                video.ErrorMessage = ex.Message;
                video.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            throw;
        }
    }

    public async Task<VideoProcessingProgress> GetProcessingProgressAsync(string videoId)
    {
        try
        {
             var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
                throw new ArgumentException($"Video not found: {videoId}");

            var jobs = await _context.Jobs
                .Where(j => j.VideoId == videoId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var stages = new List<ProcessingStage>
            {
                new ProcessingStage { Name = "download", Status = GetStageStatus(video.ProcessingProgress, 0, 25), Progress = GetStageProgress(video.ProcessingProgress, 0, 25) },
                new ProcessingStage { Name = "audio_extraction", Status = GetStageStatus(video.ProcessingProgress, 25, 50), Progress = GetStageProgress(video.ProcessingProgress, 25, 50) },
                new ProcessingStage { Name = "transcription", Status = GetStageStatus(video.ProcessingProgress, 50, 80), Progress = GetStageProgress(video.ProcessingProgress, 50, 80) },
                new ProcessingStage { Name = "embedding", Status = GetStageStatus(video.ProcessingProgress, 80, 100), Progress = GetStageProgress(video.ProcessingProgress, 80, 100) }
            };

            return new VideoProcessingProgress
            {
                VideoId = videoId,
                Status = video.Status,
                OverallProgress = video.ProcessingProgress,
                CurrentStage = GetCurrentStage(video.ProcessingProgress),
                Stages = stages,
                EstimatedCompletion = EstimateCompletion(video.ProcessingProgress),
                ErrorMessage = video.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processing progress for video: {VideoId}", videoId);
            throw;
        }
    }

    public async Task<bool> CancelProcessingAsync(string videoId)
    {
        try
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
                return false;

            if (video.Status == VideoStatus.Processing)
            {
                video.Status = VideoStatus.Failed;
                video.ErrorMessage = "Processing cancelled by user";
                video.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Cancel related jobs
                var activeJobs = await _context.Jobs
                    .Where(j => j.VideoId == videoId && j.Status == JobStatus.Running)
                    .ToListAsync();

                foreach (var job in activeJobs)
                {
                    await _jobService.CancelJobAsync(job.Id);
                }

                _logger.LogInformation("Cancelled processing for video: {VideoId}", videoId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling processing for video: {VideoId}", videoId);
            return false;
        }
    }

    private async Task ProcessVideoInBackgroundAsync(string videoId, string url)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var transcriptionService = scope.ServiceProvider.GetRequiredService<ITranscriptionService>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        try
        {
            var video = await context.Videos.FindAsync(videoId);
            if (video == null) return;

            video.Status = VideoStatus.Processing;
            await UpdateVideoProgressScoped(context, video, 5, "Starting download...");

            // Download audio
            var audioPath = await _youTubeService.DownloadAudioAsync(url, _downloadPath);
            video.AudioPath = audioPath;
            await UpdateVideoProgressScoped(context, video, 25, "Audio downloaded");

            // Extract audio if needed (already done by YouTube service)
            await UpdateVideoProgressScoped(context, video, 50, "Starting transcription...");

            // Transcribe audio
            var transcription = await transcriptionService.TranscribeAudioAsync(audioPath);
            await UpdateVideoProgressScoped(context, video, 70, "Transcription completed");

            // Save transcript segments
            var segments = await transcriptionService.ProcessTranscriptionAsync(videoId, transcription);
            context.TranscriptSegments.AddRange(segments);
            await context.SaveChangesAsync();
            await UpdateVideoProgressScoped(context, video, 80, "Processing embeddings...");

            // Generate embeddings - TODO: Use EmbeddingJobProcessor instead
            // var segmentTexts = segments.Select(s => s.Text).ToList();
            // await embeddingService.IndexTranscriptSegmentsAsync(videoId, segmentTexts);

            // Complete processing
            video.Status = VideoStatus.Completed;
            await UpdateVideoProgressScoped(context, video, 100, "Processing completed");

            _logger.LogInformation("Successfully completed processing for video: {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background processing for video: {VideoId}", videoId);

            var video = await context.Videos.FindAsync(videoId);
            if (video != null)
            {
                video.Status = VideoStatus.Failed;
                video.ErrorMessage = ex.Message;
                video.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
    }

    private async Task ProcessVideoFileInBackgroundAsync(string videoId, string filePath)
    {
        try
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null) return;

            video.Status = VideoStatus.Processing;
            await UpdateVideoProgress(video, 5, "Processing file...");

            // For uploaded files, we'd need to extract audio using FFMpeg
            // This is a simplified version - you'd want to implement proper audio extraction
            await UpdateVideoProgress(video, 25, "Audio extraction completed");

            // Transcribe audio (assuming we have audio file)
            await UpdateVideoProgress(video, 50, "Starting transcription...");

            // For demo purposes, we'll simulate this step
            // In reality, you'd extract audio first, then transcribe
            await Task.Delay(2000); // Simulate processing time

            await UpdateVideoProgress(video, 80, "Processing embeddings...");
            await Task.Delay(1000); // Simulate processing time

            video.Status = VideoStatus.Completed;
            await UpdateVideoProgress(video, 100, "Processing completed");

            _logger.LogInformation("Successfully completed file processing for video: {VideoId}", videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background file processing for video: {VideoId}", videoId);

            var video = await _context.Videos.FindAsync(videoId);
            if (video != null)
            {
                video.Status = VideoStatus.Failed;
                video.ErrorMessage = ex.Message;
                video.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task UpdateVideoProgress(Video video, int progress, string message)
    {
        video.ProcessingProgress = progress;
        video.ProcessingLog = (video.ProcessingLog ?? "") + $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {message}\n";
        video.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task UpdateVideoProgressScoped(ApplicationDbContext context, Video video, int progress, string message)
    {
        video.ProcessingProgress = progress;
        video.ProcessingLog = (video.ProcessingLog ?? "") + $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {message}\n";
        video.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private string GetStageStatus(int overallProgress, int stageStart, int stageEnd)
    {
        if (overallProgress < stageStart) return "pending";
        if (overallProgress >= stageEnd) return "completed";
        return "running";
    }

    private int GetStageProgress(int overallProgress, int stageStart, int stageEnd)
    {
        if (overallProgress < stageStart) return 0;
        if (overallProgress >= stageEnd) return 100;
        return (int)((double)(overallProgress - stageStart) / (stageEnd - stageStart) * 100);
    }

    private string GetCurrentStage(int progress)
    {
        return progress switch
        {
            < 25 => "download",
            < 50 => "audio_extraction",
            < 80 => "transcription",
            < 100 => "embedding",
            _ => "completed"
        };
    }

    private DateTime? EstimateCompletion(int progress)
    {
        if (progress >= 100) return null;

        var remainingProgress = 100 - progress;
        var estimatedMinutes = remainingProgress * 0.1; // Rough estimate: 0.1 minutes per percent
        return DateTime.UtcNow.AddMinutes(estimatedMinutes);
    }
}