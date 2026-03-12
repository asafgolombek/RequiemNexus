using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Abstract base class for homebrew entity services, implementing shared Get/Create/Delete
/// logic via the Template Method Pattern. Concrete subclasses supply entity-specific
/// query filters, ownership checks, and entity construction.
/// </summary>
/// <typeparam name="TEntity">The EF Core entity type managed by this service.</typeparam>
public abstract class HomebrewServiceBase<TEntity>
    where TEntity : class
{
    /// <summary>Initializes a new instance of <see cref="HomebrewServiceBase{TEntity}"/>.</summary>
    /// <param name="dbContext">The EF Core database context.</param>
    /// <param name="logger">The logger for structured output.</param>
    protected HomebrewServiceBase(ApplicationDbContext dbContext, ILogger logger)
    {
        DbContext = dbContext;
        Logger = logger;
    }

    /// <summary>Gets the EF Core database context.</summary>
    protected ApplicationDbContext DbContext { get; }

    /// <summary>Gets the logger for structured output.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the human-readable entity type name used in log and error messages.</summary>
    protected abstract string EntityTypeName { get; }

    /// <summary>Returns the <see cref="DbSet{TEntity}"/> for this entity type.</summary>
    protected abstract DbSet<TEntity> GetDbSet();

    /// <summary>Returns a query filtered to homebrew entities owned by <paramref name="userId"/>.</summary>
    /// <param name="userId">The user whose homebrew entities to return.</param>
    protected abstract IQueryable<TEntity> QueryByOwner(string userId);

    /// <summary>Returns <c>true</c> when <paramref name="entity"/> was authored by <paramref name="userId"/>.</summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="userId">The user claiming ownership.</param>
    protected abstract bool IsOwnedBy(TEntity entity, string userId);

    /// <summary>Gets the display name of <paramref name="entity"/> for structured log messages.</summary>
    /// <param name="entity">The entity to inspect.</param>
    protected abstract string GetName(TEntity entity);

    /// <summary>Gets the primary key of <paramref name="entity"/> for structured log messages.</summary>
    /// <param name="entity">The entity to inspect.</param>
    protected abstract int GetId(TEntity entity);

    /// <summary>Returns all homebrew entities owned by <paramref name="userId"/>.</summary>
    /// <param name="userId">The requesting user.</param>
    protected async Task<List<TEntity>> GetAllCoreAsync(string userId)
        => await QueryByOwner(userId).AsNoTracking().ToListAsync();

    /// <summary>Persists <paramref name="entity"/>, saves, and emits a structured log entry.</summary>
    /// <param name="entity">The new entity to persist.</param>
    /// <param name="userId">The author, used in the log message.</param>
    protected async Task<TEntity> SaveCoreAsync(TEntity entity, string userId)
    {
        GetDbSet().Add(entity);
        await DbContext.SaveChangesAsync();

        Logger.LogInformation(
            "Homebrew {EntityType} '{Name}' (Id={Id}) created by user {UserId}",
            EntityTypeName,
            GetName(entity),
            GetId(entity),
            userId);

        return entity;
    }

    /// <summary>
    /// Deletes the entity with <paramref name="id"/>. Throws <see cref="UnauthorizedAccessException"/>
    /// if <paramref name="userId"/> is not the homebrew author.
    /// </summary>
    /// <param name="id">The primary key of the entity to delete.</param>
    /// <param name="userId">The requesting user (must be the author).</param>
    protected async Task DeleteCoreAsync(int id, string userId)
    {
        TEntity entity = await GetDbSet().FindAsync(id)
            ?? throw new InvalidOperationException($"{EntityTypeName} {id} not found.");

        if (!IsOwnedBy(entity, userId))
        {
            throw new UnauthorizedAccessException(
                $"Only the homebrew author may delete this {EntityTypeName.ToLowerInvariant()}.");
        }

        GetDbSet().Remove(entity);
        await DbContext.SaveChangesAsync();
    }
}
