using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Infrastructure.Data;

namespace YoutubeRag.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation providing basic CRUD operations for entities
/// </summary>
/// <typeparam name="T">The entity type that inherits from BaseEntity</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the Repository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public Repository(ApplicationDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbSet = _context.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        }

        try
        {
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _dbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        try
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding entities of type {EntityType} with predicate", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            // Ensure ID is set if not provided
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                entity.Id = Guid.NewGuid().ToString();
            }

            await _dbSet.AddAsync(entity);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual Task UpdateAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity of type {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        }

        try
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        }

        try
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            if (predicate == null)
            {
                return await _dbSet.CountAsync();
            }

            return await _dbSet.CountAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets a queryable for the entity set
    /// </summary>
    /// <returns>IQueryable for the entity</returns>
    protected IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    /// <summary>
    /// Gets a no-tracking queryable for the entity set
    /// </summary>
    /// <returns>No-tracking IQueryable for the entity</returns>
    protected IQueryable<T> GetQueryableAsNoTracking()
    {
        return _dbSet.AsNoTracking();
    }
}