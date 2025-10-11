namespace YoutubeRag.Application.DTOs.Common;

/// <summary>
/// Represents a paginated result containing a list of items with pagination metadata
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public record PaginatedResultDto<T>
{
    /// <summary>
    /// Gets the collection of items for the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = new List<T>();

    /// <summary>
    /// Gets the current page number (1-based)
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the size of each page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of items across all pages
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Gets the index of the first item on the current page
    /// </summary>
    public int FirstItemIndex => PageSize * (PageNumber - 1) + 1;

    /// <summary>
    /// Gets the index of the last item on the current page
    /// </summary>
    public int LastItemIndex => Math.Min(PageSize * PageNumber, TotalCount);

    /// <summary>
    /// Creates a new instance of PaginatedResultDto
    /// </summary>
    public PaginatedResultDto(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items?.ToList() ?? new List<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public PaginatedResultDto()
    {
    }
}
