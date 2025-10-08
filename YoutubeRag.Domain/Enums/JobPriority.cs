namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Represents the priority level of a job
/// </summary>
public enum JobPriority
{
    /// <summary>
    /// Low priority job
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority job (default)
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority job
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority job
    /// </summary>
    Critical = 3
}