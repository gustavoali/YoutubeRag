using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Unit of Work pattern implementation for managing database transactions and repository access
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    // Lazy-loaded repositories
    private IUserRepository? _userRepository;
    private IVideoRepository? _videoRepository;
    private IJobRepository? _jobRepository;
    private ITranscriptSegmentRepository? _transcriptSegmentRepository;
    private IRefreshTokenRepository? _refreshTokenRepository;

    /// <summary>
    /// Initializes a new instance of the UnitOfWork class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="loggerFactory">The logger factory for creating repository loggers</param>
    public UnitOfWork(ApplicationDbContext context, ILoggerFactory loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<UnitOfWork>();
    }

    /// <inheritdoc />
    public IUserRepository Users => _userRepository ??= new UserRepository(
        _context,
        _loggerFactory.CreateLogger<UserRepository>());

    /// <inheritdoc />
    public IVideoRepository Videos => _videoRepository ??= new VideoRepository(
        _context,
        _loggerFactory.CreateLogger<VideoRepository>());

    /// <inheritdoc />
    public IJobRepository Jobs => _jobRepository ??= new JobRepository(
        _context,
        _loggerFactory.CreateLogger<JobRepository>());

    /// <inheritdoc />
    public ITranscriptSegmentRepository TranscriptSegments => _transcriptSegmentRepository ??= new TranscriptSegmentRepository(
        _context,
        _loggerFactory.CreateLogger<TranscriptSegmentRepository>());

    /// <inheritdoc />
    public IRefreshTokenRepository RefreshTokens => _refreshTokenRepository ??= new RefreshTokenRepository(
        _context,
        _loggerFactory.CreateLogger<RefreshTokenRepository>());

    /// <inheritdoc />
    public bool HasActiveTransaction => _currentTransaction != null;


    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} changes to the database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to the database");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            _logger.LogWarning("Transaction already in progress");
            return;
        }

        try
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Started new database transaction with ID {TransactionId}", _currentTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning database transaction");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            _logger.LogWarning("No transaction to commit");
            return;
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Committed transaction with ID {TransactionId}", _currentTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction with ID {TransactionId}", _currentTransaction.TransactionId);
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            _logger.LogWarning("No transaction to rollback");
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Rolled back transaction with ID {TransactionId}", _currentTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction with ID {TransactionId}", _currentTransaction.TransactionId);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        // If there's already a transaction, just execute the operation
        if (HasActiveTransaction)
        {
            await operation();
            return;
        }

        // Start a new transaction
        await BeginTransactionAsync(cancellationToken);
        try
        {
            await operation();
            await SaveChangesAsync(cancellationToken);
            await CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing operation in transaction");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        // If there's already a transaction, just execute the operation
        if (HasActiveTransaction)
        {
            return await operation();
        }

        // Start a new transaction
        await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await SaveChangesAsync(cancellationToken);
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing operation in transaction");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Disposes the current transaction
    /// </summary>
    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Disposes the unit of work and its resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _currentTransaction?.Dispose();
                _context.Dispose();
            }

            _disposed = true;
        }
    }
}