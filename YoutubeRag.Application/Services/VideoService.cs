using AutoMapper;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.DTOs.Common;
using YoutubeRag.Application.DTOs.Video;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Service implementation for video management operations
/// </summary>
public class VideoService : IVideoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VideoService> _logger;

    public VideoService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VideoService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VideoDto?> GetByIdAsync(string id)
    {
        _logger.LogInformation("Getting video by ID: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            _logger.LogWarning("Video not found: {VideoId}", id);
            return null;
        }

        return _mapper.Map<VideoDto>(video);
    }

    public async Task<PaginatedResultDto<VideoListDto>> GetAllAsync(int page = 1, int pageSize = 10, string? userId = null)
    {
        _logger.LogInformation("Getting all videos - Page: {Page}, PageSize: {PageSize}, UserId: {UserId}",
            page, pageSize, userId ?? "all");

        IEnumerable<Video> videos;
        int totalCount;

        if (!string.IsNullOrEmpty(userId))
        {
            videos = await _unitOfWork.Videos.FindAsync(v => v.UserId == userId);
            totalCount = videos.Count();
            videos = videos.Skip((page - 1) * pageSize).Take(pageSize);
        }
        else
        {
            totalCount = await _unitOfWork.Videos.CountAsync();
            var allVideos = await _unitOfWork.Videos.GetAllAsync();
            videos = allVideos.Skip((page - 1) * pageSize).Take(pageSize);
        }

        var videoDtos = _mapper.Map<List<VideoListDto>>(videos);

        return new PaginatedResultDto<VideoListDto>(
            videoDtos,
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<VideoDto> CreateAsync(CreateVideoDto createDto, string userId)
    {
        _logger.LogInformation("Creating new video: {Title} for user: {UserId}", createDto.Title, userId);

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        var video = _mapper.Map<Video>(createDto);
        video.Id = Guid.NewGuid().ToString();
        video.UserId = userId;
        video.CreatedAt = DateTime.UtcNow;
        video.UpdatedAt = DateTime.UtcNow;
        video.Status = Domain.Enums.VideoStatus.Pending;

        await _unitOfWork.Videos.AddAsync(video);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Video created successfully: {VideoId}", video.Id);

        return _mapper.Map<VideoDto>(video);
    }

    public async Task<VideoDto> UpdateAsync(string id, UpdateVideoDto updateDto)
    {
        _logger.LogInformation("Updating video: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(updateDto.Title))
        {
            video.Title = updateDto.Title;
        }

        if (updateDto.Description != null)
        {
            video.Description = updateDto.Description;
        }

        if (updateDto.Status.HasValue)
        {
            video.Status = updateDto.Status.Value;
        }

        if (updateDto.Duration.HasValue)
        {
            video.Duration = updateDto.Duration.Value;
        }

        video.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Videos.UpdateAsync(video);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Video updated successfully: {VideoId}", id);

        return _mapper.Map<VideoDto>(video);
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Deleting video: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        await _unitOfWork.Videos.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Video deleted successfully: {VideoId}", id);
    }

    public async Task<VideoDetailsDto> GetDetailsAsync(string id)
    {
        _logger.LogInformation("Getting video details: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        // Load related entities manually
        // Note: In a real implementation, you might want to use Include/ThenInclude with EF Core
        // For now, the mapper will handle the navigation properties if they're loaded

        return _mapper.Map<VideoDetailsDto>(video);
    }

    public async Task<VideoStatsDto> GetStatsAsync(string id)
    {
        _logger.LogInformation("Getting stats for video: {VideoId}", id);

        var video = await _unitOfWork.Videos.GetByIdAsync(id);
        if (video == null)
        {
            throw new EntityNotFoundException("Video", id);
        }

        var transcripts = await _unitOfWork.TranscriptSegments.FindAsync(t => t.VideoId == id);
        var jobs = await _unitOfWork.Jobs.FindAsync(j => j.VideoId == id);

        var stats = new VideoStatsDto(
            Id: id,
            TotalTranscriptSegments: transcripts.Count(),
            TotalJobs: jobs.Count(),
            CompletedJobs: jobs.Count(j => j.Status == Domain.Enums.JobStatus.Completed),
            FailedJobs: jobs.Count(j => j.Status == Domain.Enums.JobStatus.Failed),
            AverageConfidence: transcripts.Any() ? transcripts.Average(t => t.Confidence ?? 0) : 0,
            Duration: video.Duration
        );

        return stats;
    }

    public async Task<List<VideoListDto>> GetByUserIdAsync(string userId)
    {
        _logger.LogInformation("Getting videos for user: {UserId}", userId);

        var videos = await _unitOfWork.Videos.FindAsync(v => v.UserId == userId);
        return _mapper.Map<List<VideoListDto>>(videos);
    }
}
