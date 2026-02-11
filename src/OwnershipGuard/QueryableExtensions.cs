using System.Linq.Expressions;

namespace OwnershipGuard.EntityFrameworkCore;

/// <summary>
/// EF Core extension methods to scope queries by owner or tenant and reduce mistakes.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Filters the query to entities owned by the specified user.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Source query.</param>
    /// <param name="userId">Owner id to filter by.</param>
    /// <param name="ownerSelector">Expression for the entity's owner id property.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereOwnedBy<T>(
        this IQueryable<T> query,
        string userId,
        Expression<Func<T, string>> ownerSelector)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(ownerSelector);
        var param = ownerSelector.Parameters[0];
        var body = Expression.Equal(ownerSelector.Body, Expression.Constant(userId, typeof(string)));
        var predicate = Expression.Lambda<Func<T, bool>>(body, param);
        return query.Where(predicate);
    }

    /// <summary>
    /// Filters the query to entities belonging to the specified tenant.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">Source query.</param>
    /// <param name="tenantId">Tenant id to filter by.</param>
    /// <param name="tenantSelector">Expression for the entity's tenant id property.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<T> WhereTenant<T>(
        this IQueryable<T> query,
        string tenantId,
        Expression<Func<T, string>> tenantSelector)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(tenantSelector);
        var param = tenantSelector.Parameters[0];
        var body = Expression.Equal(tenantSelector.Body, Expression.Constant(tenantId, typeof(string)));
        var predicate = Expression.Lambda<Func<T, bool>>(body, param);
        return query.Where(predicate);
    }
}
