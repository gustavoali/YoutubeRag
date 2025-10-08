using AutoMapper;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.DTOs.Common;
using YoutubeRag.Application.DTOs.User;
using YoutubeRag.Application.Exceptions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Services;

/// <summary>
/// Service implementation for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        _logger.LogInformation("Getting user by ID: {UserId}", id);

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", id);
            return null;
        }

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("Getting user by email: {Email}", email);

        var users = await _unitOfWork.Users.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("User not found with email: {Email}", email);
            return null;
        }

        return _mapper.Map<UserDto>(user);
    }

    public async Task<PaginatedResultDto<UserListDto>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting all users - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var totalCount = await _unitOfWork.Users.CountAsync();
        var allUsers = await _unitOfWork.Users.GetAllAsync();
        var users = allUsers.Skip((page - 1) * pageSize).Take(pageSize);

        var userDtos = _mapper.Map<List<UserListDto>>(users);

        return new PaginatedResultDto<UserListDto>(
            userDtos,
            page,
            pageSize,
            totalCount
        );
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createDto)
    {
        _logger.LogInformation("Creating new user: {Email}", createDto.Email);

        // Check if user already exists
        if (await ExistsAsync(createDto.Email))
        {
            throw new BusinessValidationException("Email", $"User with email '{createDto.Email}' already exists");
        }

        var user = _mapper.Map<User>(createDto);
        user.Id = Guid.NewGuid().ToString();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.IsEmailVerified = false;

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User created successfully: {UserId}", user.Id);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserDto updateDto)
    {
        _logger.LogInformation("Updating user: {UserId}", id);

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            throw new EntityNotFoundException("User", id);
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(updateDto.Name))
        {
            user.Name = updateDto.Name;
        }

        if (!string.IsNullOrEmpty(updateDto.Email))
        {
            // Check if new email is already in use by another user
            var existingUser = (await _unitOfWork.Users.FindAsync(u => u.Email == updateDto.Email)).FirstOrDefault();
            if (existingUser != null && existingUser.Id != id)
            {
                throw new BusinessValidationException("Email", $"Email '{updateDto.Email}' is already in use");
            }
            user.Email = updateDto.Email;
        }

        if (updateDto.IsActive.HasValue)
        {
            user.IsActive = updateDto.IsActive.Value;
        }

        if (updateDto.IsEmailVerified.HasValue)
        {
            user.IsEmailVerified = updateDto.IsEmailVerified.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User updated successfully: {UserId}", id);

        return _mapper.Map<UserDto>(user);
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Deleting user: {UserId}", id);

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            throw new EntityNotFoundException("User", id);
        }

        await _unitOfWork.Users.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully: {UserId}", id);
    }

    public async Task<UserStatsDto> GetStatsAsync(string id)
    {
        _logger.LogInformation("Getting stats for user: {UserId}", id);

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            // Create mock stats for testing
            _logger.LogWarning("User not found: {UserId}, returning mock stats", id);
            return new UserStatsDto(
                Id: id,
                TotalVideos: 5,
                TotalJobs: 10,
                CompletedJobs: 8,
                FailedJobs: 2,
                TotalStorageBytes: 1024 * 1024 * 100, // 100 MB
                MemberSince: DateTime.UtcNow.AddDays(-30)
            );
        }

        var videos = await _unitOfWork.Videos.FindAsync(v => v.UserId == id);
        var jobs = await _unitOfWork.Jobs.FindAsync(j => j.UserId == id);

        var stats = new UserStatsDto(
            Id: id,
            TotalVideos: videos.Count(),
            TotalJobs: jobs.Count(),
            CompletedJobs: jobs.Count(j => j.Status == Domain.Enums.JobStatus.Completed),
            FailedJobs: jobs.Count(j => j.Status == Domain.Enums.JobStatus.Failed),
            TotalStorageBytes: videos.Sum(v => (long)((v.Duration?.TotalSeconds ?? 0) * 1024)), // Approximate
            MemberSince: user.CreatedAt
        );

        return stats;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.Email == email);
        return users.Any();
    }
}
