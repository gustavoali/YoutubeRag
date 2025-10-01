using System.ComponentModel.DataAnnotations;

namespace YoutubeRag.Application.DTOs.Common;

/// <summary>
/// Represents pagination parameters for list requests
/// </summary>
public record PaginationRequestDto
{
    /// <summary>
    /// Gets the page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets the page size
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Gets the field to sort by
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets whether to sort in descending order
    /// </summary>
    public bool SortDescending { get; init; } = false;

    /// <summary>
    /// Gets the search query
    /// </summary>
    public string? SearchQuery { get; init; }

    /// <summary>
    /// Gets the skip count for pagination
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Default constructor
    /// </summary>
    public PaginationRequestDto()
    {
    }

    /// <summary>
    /// Creates a new PaginationRequestDto with specified values
    /// </summary>
    public PaginationRequestDto(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}