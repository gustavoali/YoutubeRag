using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.DTOs.Transcription;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using Hangfire;

namespace YoutubeRag.Infrastructure.Jobs;

/// <summary>
/// Processor for the Segmentation stage of the transcription pipeline
/// Stores transcript segments in database
/// </summary>
public class SegmentationJobProcessor
{
    private readonly ITranscriptSegmentRepository _transcriptSegmentRepository;
    private readonly ISegmentationService _segmentationService;
    private readonly IVideoRepository _videoRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SegmentationJobProcessor> _logger;

    public SegmentationJobProcessor(
        ITranscriptSegmentRepository transcriptSegmentRepository,
        ISegmentationService segmentationService,
        IVideoRepository videoRepository,
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ILogger<SegmentationJobProcessor> logger)
    {
        _transcriptSegmentRepository = transcriptSegmentRepository;
        _segmentationService = segmentationService;
        _videoRepository = videoRepository;
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executes the segmentation stage for a video job
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 0)] // We handle retries ourselves with retry policies
    [Queue("default")]
    public async Task ExecuteAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Segmentation stage for job: {JobId}", jobId);

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            throw new InvalidOperationException($"Job {jobId} not found");
        }

        var video = await _videoRepository.GetByIdAsync(job.VideoId!);
        if (video == null)
        {
            _logger.LogError("Video {VideoId} not found for job {JobId}", job.VideoId, jobId);
            throw new InvalidOperationException($"Video {job.VideoId} not found");
        }

        try
        {
            // Update job stage
            job.CurrentStage = PipelineStage.Segmentation;
            job.Status = JobStatus.Running;
            job.SetStageProgress(PipelineStage.Segmentation, 0);
            job.Progress = job.CalculateOverallProgress();
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Get transcription result from metadata
            var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(job.Metadata ?? "{}")
                ?? new Dictionary<string, object>();

            if (!metadata.TryGetValue("TranscriptionResultJson", out var resultJsonObj))
            {
                throw new InvalidOperationException("Transcription result not found in job metadata");
            }

            var transcriptionResult = System.Text.Json.JsonSerializer.Deserialize<TranscriptionResultDto>(
                resultJsonObj.ToString()!);

            if (transcriptionResult == null)
            {
                throw new InvalidOperationException("Failed to deserialize transcription result");
            }

            _logger.LogInformation("Storing {Count} transcript segments for job {JobId}",
                transcriptionResult.Segments.Count, jobId);

            // Delete existing segments if any
            var existingSegmentsCount = await _transcriptSegmentRepository.DeleteByVideoIdAsync(video.Id);
            if (existingSegmentsCount > 0)
            {
                _logger.LogInformation("Deleted {Count} existing transcript segments for video {VideoId}",
                    existingSegmentsCount, video.Id);
            }

            // Create new segments list for bulk insert
            var allSegments = new List<TranscriptSegment>();
            var now = DateTime.UtcNow;
            const int MAX_SEGMENT_LENGTH = 500;

            // Process each segment from Whisper output
            for (int i = 0; i < transcriptionResult.Segments.Count; i++)
            {
                var segmentDto = transcriptionResult.Segments[i];
                var trimmedText = segmentDto.Text.Trim();

                // Check if segment is too long and needs splitting
                if (trimmedText.Length > MAX_SEGMENT_LENGTH)
                {
                    _logger.LogDebug("Segment {Index} is too long ({Length} chars). Splitting into smaller segments.",
                        i, trimmedText.Length);

                    // Use SegmentationService to split long segment
                    var subSegments = await _segmentationService.CreateSegmentsFromTranscriptAsync(
                        video.Id,
                        trimmedText,
                        segmentDto.StartTime,
                        segmentDto.EndTime,
                        MAX_SEGMENT_LENGTH
                    );

                    // Update metadata for sub-segments
                    foreach (var subSegment in subSegments)
                    {
                        subSegment.Language = transcriptionResult.Language;
                        subSegment.Confidence = segmentDto.Confidence;
                        subSegment.Speaker = segmentDto.Speaker;
                        subSegment.CreatedAt = now;
                        subSegment.UpdatedAt = now;
                    }

                    allSegments.AddRange(subSegments);
                }
                else
                {
                    // Segment is already the right size
                    var segment = new TranscriptSegment
                    {
                        Id = Guid.NewGuid().ToString(),
                        VideoId = video.Id,
                        SegmentIndex = i,
                        StartTime = segmentDto.StartTime,
                        EndTime = segmentDto.EndTime,
                        Text = trimmedText,
                        Language = transcriptionResult.Language,
                        Confidence = segmentDto.Confidence,
                        Speaker = segmentDto.Speaker,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    allSegments.Add(segment);
                }

                // Update progress periodically
                if (i % 10 == 0)
                {
                    var progress = (double)i / transcriptionResult.Segments.Count * 100;
                    job.SetStageProgress(PipelineStage.Segmentation, progress);
                    job.Progress = job.CalculateOverallProgress();
                    await _jobRepository.UpdateAsync(job);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // Re-index all segments sequentially
            for (int i = 0; i < allSegments.Count; i++)
            {
                allSegments[i].SegmentIndex = i;
            }

            // Bulk insert all segments at once
            if (allSegments.Any())
            {
                await _transcriptSegmentRepository.AddRangeAsync(allSegments, cancellationToken);
                _logger.LogInformation("Bulk inserted {Count} transcript segments for video {VideoId} (from {OriginalCount} Whisper segments)",
                    allSegments.Count, video.Id, transcriptionResult.Segments.Count);
            }

            // Mark segmentation stage as complete
            job.SetStageProgress(PipelineStage.Segmentation, 100);
            job.Progress = 100;
            job.CurrentStage = PipelineStage.Completed;
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.StatusMessage = $"Successfully transcribed {allSegments.Count} segments";

            // Update video status
            video.ProcessingStatus = VideoStatus.Completed;
            video.TranscriptionStatus = TranscriptionStatus.Completed;
            video.TranscribedAt = DateTime.UtcNow;
            video.UpdatedAt = DateTime.UtcNow;

            await _videoRepository.UpdateAsync(video);
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Segmentation stage completed successfully for job {JobId}. Total segments: {Count}",
                jobId, allSegments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Segmentation stage failed for job {JobId}: {Error}", jobId, ex.Message);

            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.SetStageProgress(PipelineStage.Segmentation, 0);
            await _jobRepository.UpdateAsync(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}
