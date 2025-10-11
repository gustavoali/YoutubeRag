using System.Linq.Expressions;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Application.Interfaces;

/// <summary>
/// Generic repository interface providing basic CRUD operations for entities
/// </summary>
/// <typeparam name="T">The entity type that inherits from BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Gets an entity by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the entity</param>
    /// <returns>The entity if found; otherwise, null</returns>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Gets all entities from the repository
    /// </summary>
    /// <returns>A collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Finds entities that match the specified predicate
    /// </summary>
    /// <param name="predicate">The condition to filter entities</param>
    /// <returns>A collection of entities that match the predicate</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Adds a new entity to the repository
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <returns>The added entity</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity in the repository
    /// </summary>
    /// <param name="entity">The entity with updated values</param>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity from the repository by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete</param>
    Task DeleteAsync(string id);

    /// <summary>
    /// Checks if an entity with the specified identifier exists
    /// </summary>
    /// <param name="id">The unique identifier to check</param>
    /// <returns>True if the entity exists; otherwise, false</returns>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Counts the total number of entities that match the specified predicate
    /// </summary>
    /// <param name="predicate">Optional condition to filter entities before counting</param>
    /// <returns>The count of entities matching the predicate</returns>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}
