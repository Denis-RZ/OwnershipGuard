using System.Linq.Expressions;

namespace OwnershipGuard;

/// <summary>
/// Performs ownership and access checks to prevent IDOR / broken access control.
/// </summary>
public interface IAccessGuard
{
    /// <summary>
    /// Checks whether the specified user owns the resource identified by <paramref name="resourceId"/>.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Queryable data source (e.g. DbSet).</param>
    /// <param name="resourceId">Id of the resource.</param>
    /// <param name="userId">Current user id (e.g. from claims).</param>
    /// <param name="idSelector">Expression to get the entity's id property.</param>
    /// <param name="ownerSelector">Expression to get the entity's owner id property.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user owns the resource.</returns>
    Task<bool> IsOwnerAsync<T>(
        IQueryable<T> query,
        string resourceId,
        string userId,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requires that the user owns the resource. Returns a result indicating success or which status to return (403/404).
    /// </summary>
    Task<RequireOwnerResult> RequireOwnerAsync<T>(
        IQueryable<T> query,
        string resourceId,
        string userId,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requires that the user owns the resource identified by <paramref name="resourceId"/> (typed key).
    /// Use when the entity id is Guid, int, long, etc. Resource that does not exist returns NotFound; exists but not owned returns Forbidden (or NotFound in hide mode).
    /// </summary>
    Task<RequireOwnerResult> RequireOwnerAsync<T, TKey>(
        IQueryable<T> query,
        TKey resourceId,
        string userId,
        Expression<Func<T, TKey>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        CancellationToken cancellationToken = default)
        where TKey : IParsable<TKey>;

    /// <summary>
    /// Requires that the user owns the resource and that it belongs to the specified tenant (string id).
    /// Returns NotFound if the resource does not exist; otherwise Success or Forbidden (or NotFound in hide mode).
    /// </summary>
    Task<RequireOwnerResult> RequireOwnerAndTenantAsync<T>(
        IQueryable<T> query,
        string resourceId,
        string userId,
        string tenantId,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>> tenantSelector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requires that the user owns the resource and that it belongs to the specified tenant (typed key).
    /// Returns NotFound if the resource does not exist; otherwise Success or Forbidden (or NotFound in hide mode).
    /// </summary>
    Task<RequireOwnerResult> RequireOwnerAndTenantAsync<T, TKey>(
        IQueryable<T> query,
        TKey resourceId,
        string userId,
        string tenantId,
        Expression<Func<T, TKey>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>> tenantSelector,
        CancellationToken cancellationToken = default)
        where TKey : IParsable<TKey>;

    /// <summary>
    /// Requires that the user owns the resource identified by <paramref name="resourceId"/> for the registered entity type.
    /// Resolves the descriptor from the registry and runs the check. Used by the endpoint filter.
    /// </summary>
    /// <param name="entityType">Entity type registered with <see cref="IOwnershipDescriptorRegistry"/>.</param>
    /// <param name="resourceId">Id of the resource.</param>
    /// <param name="userId">Current user id (e.g. from claims).</param>
    /// <param name="serviceProvider">Request service provider (e.g. <see cref="Microsoft.AspNetCore.Http.HttpContext.RequestServices"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, Forbidden, or NotFound.</returns>
    Task<RequireOwnerResult> RequireOwnerAsync(
        Type entityType,
        string resourceId,
        string userId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requires that the user owns the resource identified by <paramref name="resourceId"/> and that it belongs to the specified tenant
    /// for the registered entity type.
    /// Resolves the tenant-aware descriptor from the registry and runs the check. Used by endpoint/MVC filters when tenant checks are registered.
    /// </summary>
    Task<RequireOwnerResult> RequireOwnerAndTenantAsync(
        Type entityType,
        string resourceId,
        string userId,
        string tenantId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);
}
