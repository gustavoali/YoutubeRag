namespace YoutubeRag.Domain.Enums;

/// <summary>
/// Represents the status of a pipeline stage execution
/// </summary>
public enum StageStatus
{
    /// <summary>
    /// Stage has not started yet
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Stage is currently executing
    /// </summary>
    Running = 1,

    /// <summary>
    /// Stage completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Stage failed during execution
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Stage was skipped (due to conditions or previous failure)
    /// </summary>
    Skipped = 4,

    /// <summary>
    /// Stage is being retried after failure
    /// </summary>
    Retrying = 5,

    /// <summary>
    /// Stage requires compensation/rollback due to later failure
    /// </summary>
    CompensationRequired = 6,

    /// <summary>
    /// Stage has been compensated/rolled back
    /// </summary>
    Compensated = 7,

    /// <summary>
    /// Stage is paused and can be resumed
    /// </summary>
    Paused = 8,

    /// <summary>
    /// Stage was cancelled by user or system
    /// </summary>
    Cancelled = 9
}