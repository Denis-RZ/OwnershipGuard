using System.Linq.Expressions;

namespace OwnershipGuard;

/// <summary>
/// Registry of ownership descriptors per entity type. Used by the endpoint filter to resolve query and selectors.
/// </summary>
public interface IOwnershipDescriptorRegistry
{
    /// <summary>
    /// Registers an ownership descriptor for entity type <typeparamref name="T"/> with string id.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="getQuery">Factory to get the queryable for the current request (e.g. from DbContext).</param>
    /// <param name="idSelector">Expression for the entity's id property.</param>
    /// <param name="ownerSelector">Expression for the entity's owner id property.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity type is already registered (duplicate registration).</exception>
    void Register<T>(
        Func<IServiceProvider, IQueryable<T>> getQuery,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector);

    /// <summary>
    /// Registers an ownership+tenant descriptor for entity type <typeparamref name="T"/> with string id.
    /// When registered, endpoint/MVC filters can enforce both owner and tenant checks.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="getQuery">Factory to get the queryable for the current request (e.g. from DbContext).</param>
    /// <param name="idSelector">Expression for the entity's id property.</param>
    /// <param name="ownerSelector">Expression for the entity's owner id property.</param>
    /// <param name="tenantSelector">Expression for the entity's tenant id property.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity type is already registered (duplicate registration).</exception>
    void Register<T>(
        Func<IServiceProvider, IQueryable<T>> getQuery,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>> tenantSelector);

    /// <summary>
    /// Registers an ownership descriptor for entity type <typeparamref name="T"/> with typed key (e.g. Guid, int, long).
    /// The route value is parsed using <see cref="IParsable{TKey}.TryParse"/>; invalid id returns <see cref="RequireOwnerResult.InvalidId"/> (400).
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TKey">Id type (must be IParsable, e.g. Guid, int, long).</typeparam>
    /// <param name="getQuery">Factory to get the queryable for the current request.</param>
    /// <param name="idSelector">Expression for the entity's id property.</param>
    /// <param name="ownerSelector">Expression for the entity's owner id property.</param>
    /// <param name="tenantSelector">Optional tenant selector for future tenant checks.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity type is already registered (duplicate registration).</exception>
    void Register<T, TKey>(
        Func<IServiceProvider, IQueryable<T>> getQuery,
        Expression<Func<T, TKey>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>>? tenantSelector = null)
        where TKey : IParsable<TKey>;

    /// <summary>
    /// Tries to get the ownership check delegate for the given entity type.
    /// </summary>
    bool TryGetExecutor(Type entityType, out OwnershipCheckExecutor? executor);

    /// <summary>
    /// Tries to get the ownership+tenant check delegate for the given entity type.
    /// Returns false when the entity is not registered with a tenant selector.
    /// </summary>
    bool TryGetTenantExecutor(Type entityType, out OwnershipTenantCheckExecutor? executor);

    /// <summary>
    /// Gets the ownership check delegate for the given entity type.
    /// </summary>
    /// <exception cref="OwnershipDescriptorNotRegisteredException">Thrown when the descriptor is missing.</exception>
    OwnershipCheckExecutor GetExecutor(Type entityType);

    /// <summary>
    /// Gets the ownership+tenant check delegate for the given entity type.
    /// </summary>
    /// <exception cref="OwnershipDescriptorNotRegisteredException">Thrown when the tenant-aware descriptor is missing.</exception>
    OwnershipTenantCheckExecutor GetTenantExecutor(Type entityType);
}

/// <summary>
/// Delegate that runs the ownership check for a registered entity type.
/// </summary>
public delegate Task<RequireOwnerResult> OwnershipCheckExecutor(
    IAccessGuard guard,
    IServiceProvider serviceProvider,
    string resourceId,
    string userId,
    CancellationToken cancellationToken);

/// <summary>
/// Delegate that runs the ownership+tenant check for a registered entity type.
/// </summary>
public delegate Task<RequireOwnerResult> OwnershipTenantCheckExecutor(
    IAccessGuard guard,
    IServiceProvider serviceProvider,
    string resourceId,
    string userId,
    string tenantId,
    CancellationToken cancellationToken);
