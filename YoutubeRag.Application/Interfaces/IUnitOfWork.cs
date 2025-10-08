namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing database transactions and repository access
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the User repository
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the Video repository
    /// </summary>
    IVideoRepository Videos { get; }

    /// <summary>
    /// Gets the Job repository
    /// </summary>
    IJobRepository Jobs { get; }

    /// <summary>
    /// Gets the TranscriptSegment repository
    /// </summary>
    ITranscriptSegmentRepository TranscriptSegments { get; }

    /// <summary>
    /// Gets the RefreshToken repository
    /// </summary>
    IRefreshTokenRepository RefreshTokens { get; }

    /// <summary>
    /// Saves all changes made in this unit of work to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current database transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current database transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there is an active transaction
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Executes the given action within a database transaction
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the given function within a database transaction and returns the result
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}