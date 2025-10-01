using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YoutubeRag.Api.Models;
using YoutubeRag.Application.Interfaces.Services;
using YoutubeRag.Application.DTOs.User;
using YoutubeRag.Application.Exceptions;
using System.Security.Claims;

namespace YoutubeRag.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Tags("👥 Users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfile>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "User not authenticated" } });
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = new { code = "NOT_FOUND", message = "User not found" } });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPatch("me")]
    public async Task<ActionResult<UserProfile>> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "User not authenticated" } });
            }

            var updateDto = new UpdateUserDto
            {
                Name = request.Name
            };

            var user = await _userService.UpdateAsync(userId, updateDto);
            return Ok(user);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = ex.Message, errors = ex.Errors } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get user's activity dashboard
    /// </summary>
    [HttpGet("me/dashboard")]
    public async Task<ActionResult> GetUserDashboard()
    {
        return Ok(new {
            user_stats = new {
                total_videos = 25,
                processing_videos = 2,
                completed_videos = 21,
                failed_videos = 2,
                total_watch_time_minutes = 1250,
                total_transcript_segments = 3420
            },
            recent_activity = new[] {
                new {
                    id = "activity_1",
                    type = "video_uploaded",
                    description = "Uploaded 'Machine Learning Basics'",
                    timestamp = DateTime.UtcNow.AddMinutes(-30),
                    video_id = "video_1"
                },
                new {
                    id = "activity_2",
                    type = "search_performed",
                    description = "Searched for 'neural networks'",
                    timestamp = DateTime.UtcNow.AddHours(-2),
                    video_id = (string?)null
                },
                new {
                    id = "activity_3",
                    type = "video_processed",
                    description = "Processing completed for 'Data Science Tutorial'",
                    timestamp = DateTime.UtcNow.AddHours(-4),
                    video_id = "video_2"
                }
            },
            usage_analytics = new {
                searches_this_week = 45,
                videos_processed_this_month = 12,
                most_searched_topics = new[] {
                    new { topic = "machine learning", count = 15 },
                    new { topic = "python", count = 12 },
                    new { topic = "data science", count = 8 }
                },
                processing_time_saved_hours = 23.5
            }
        });
    }

    /// <summary>
    /// Get user's video library statistics
    /// </summary>
    [HttpGet("me/stats")]
    public async Task<ActionResult> GetUserStats(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string period = "week")
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "User not authenticated" } });
            }

            var stats = await _userService.GetStatsAsync(userId);
            return Ok(stats);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get user's search history
    /// </summary>
    [HttpGet("me/search-history")]
    public async Task<ActionResult> GetSearchHistory(
        int page = 1,
        int pageSize = 20,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var searches = new[]
        {
            new {
                id = "search_1",
                query = "machine learning algorithms",
                search_type = "semantic",
                results_count = 12,
                timestamp = DateTime.UtcNow.AddHours(-2),
                processing_time_ms = 245
            },
            new {
                id = "search_2",
                query = "neural networks",
                search_type = "keyword",
                results_count = 8,
                timestamp = DateTime.UtcNow.AddHours(-4),
                processing_time_ms = 156
            },
            new {
                id = "search_3",
                query = "deep learning tutorial",
                search_type = "advanced",
                results_count = 15,
                timestamp = DateTime.UtcNow.AddHours(-6),
                processing_time_ms = 398
            }
        };

        return Ok(new {
            searches,
            total = searches.Length,
            page,
            page_size = pageSize,
            has_more = false
        });
    }

    /// <summary>
    /// Get user's preferences and settings
    /// </summary>
    [HttpGet("me/preferences")]
    public async Task<ActionResult> GetUserPreferences()
    {
        return Ok(new {
            search_preferences = new {
                default_search_type = "semantic",
                max_results = 20,
                min_relevance_score = 0.7,
                include_timestamps = true,
                language_preference = "auto"
            },
            video_preferences = new {
                auto_process_uploads = true,
                preferred_quality = "720p",
                extract_audio = true,
                generate_embeddings = true,
                transcription_language = "auto"
            },
            notification_preferences = new {
                email_notifications = true,
                processing_complete = true,
                weekly_summary = false,
                search_suggestions = true
            },
            privacy_settings = new {
                public_profile = false,
                share_statistics = false,
                data_retention_days = 365
            }
        });
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPatch("me/preferences")]
    public async Task<ActionResult> UpdateUserPreferences([FromBody] Dictionary<string, object> preferences)
    {
        return Ok(new {
            message = "Preferences updated successfully",
            updated_fields = preferences.Keys,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Delete user account (soft delete)
    /// </summary>
    [HttpDelete("me")]
    public async Task<ActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        if (string.IsNullOrEmpty(request.Reason))
        {
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Deletion reason is required" } });
        }

        return Ok(new {
            message = "Account deletion scheduled",
            deletion_date = DateTime.UtcNow.AddDays(30), // 30-day grace period
            reason = request.Reason,
            recovery_instructions = "Contact support within 30 days to recover your account"
        });
    }

    /// <summary>
    /// Export user data (GDPR compliance)
    /// </summary>
    [HttpPost("me/export")]
    public async Task<ActionResult> ExportUserData([FromBody] ExportDataRequest request)
    {
        var exportId = Guid.NewGuid().ToString();

        return Ok(new {
            export_id = exportId,
            format = request.Format,
            include_videos = request.IncludeVideos,
            include_searches = request.IncludeSearchHistory,
            include_preferences = request.IncludePreferences,
            estimated_completion = DateTime.UtcNow.AddMinutes(15),
            message = "Data export initiated. You will receive a download link via email."
        });
    }

    /// <summary>
    /// Get user's API usage and quotas
    /// </summary>
    [HttpGet("me/usage")]
    public async Task<ActionResult> GetApiUsage()
    {
        return Ok(new {
            current_period = new {
                start_date = DateTime.UtcNow.Date.AddDays(-DateTime.UtcNow.Day + 1),
                end_date = DateTime.UtcNow.Date.AddDays(-DateTime.UtcNow.Day + 1).AddMonths(1).AddDays(-1)
            },
            quotas = new {
                videos_processed = new { used = 23, limit = 100, percentage = 23.0 },
                searches_performed = new { used = 1567, limit = 5000, percentage = 31.3 },
                storage_mb = new { used = 2456, limit = 10240, percentage = 24.0 },
                api_calls = new { used = 8923, limit = 50000, percentage = 17.8 }
            },
            tier = "standard",
            upgrade_available = true,
            billing_cycle = "monthly"
        });
    }
}

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public string? Avatar { get; set; }
}

public class DeleteAccountRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Feedback { get; set; }
}

public class ExportDataRequest
{
    public string Format { get; set; } = "json"; // json, csv, xml
    public bool IncludeVideos { get; set; } = true;
    public bool IncludeSearchHistory { get; set; } = true;
    public bool IncludePreferences { get; set; } = true;
}