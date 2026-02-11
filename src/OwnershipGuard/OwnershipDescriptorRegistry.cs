using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;

namespace OwnershipGuard;

internal sealed class OwnershipDescriptorRegistry : IOwnershipDescriptorRegistry
{
    private readonly ConcurrentDictionary<Type, OwnershipCheckExecutor> _executors = new();
    private readonly ConcurrentDictionary<Type, OwnershipTenantCheckExecutor> _tenantExecutors = new();

    public void Register<T>(
        Func<IServiceProvider, IQueryable<T>> getQuery,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector)
    {
        var type = typeof(T);
        OwnershipCheckExecutor executor = (guard, sp, resourceId, userId, ct) =>
            guard.RequireOwnerAsync(getQuery(sp), resourceId, userId, idSelector, ownerSelector, ct);
        if (!_executors.TryAdd(type, executor))
            throw new InvalidOperationException($"Ownership descriptor already registered for entity type: {type.FullName}.");
    }

    public void Register<T>(
        Func<IServiceProvider, IQueryable<T>> getQuery,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>> tenantSelector)
    {
        var type = typeof(T);

        OwnershipCheckExecutor executor = (guard, sp, resourceId, userId, ct) =>
            guard.RequireOwnerAsync(getQuery(sp), resourceId, userId, idSelector, ownerSelector, ct);

        OwnershipTenantCheckExecutor tenantExecutor = (guard, sp, resourceId, userId, tenantId, ct) =>
            guard.RequireOwnerAndTenantAsync(getQuery(sp), resourceId, userId, tenantId, idSelector, ownerSelector, tenantSelector, ct);

        if (!_executors.TryAdd(type, executor))
            throw new InvalidOperationException($"Ownership descriptor already registered for entity type: {type.FullName}.");
        _tenantExecutors.TryAdd(type, tenantExecutor);
    }

    public void Register<T, TKey>(
        Func<IServiceProvider, IQueryable<T>> getQuery,
        Expression<Func<T, TKey>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>>? tenantSelector)
        where TKey : IParsable<TKey>
    {
        var type = typeof(T);

        OwnershipCheckExecutor executor = (guard, sp, rawId, userId, ct) =>
            TKey.TryParse(rawId, CultureInfo.InvariantCulture, out var key)
                ? guard.RequireOwnerAsync(getQuery(sp), key, userId, idSelector, ownerSelector, ct)
                : Task.FromResult(RequireOwnerResult.InvalidId);

        OwnershipTenantCheckExecutor? tenantExecutor = null;
        if (tenantSelector != null)
        {
            tenantExecutor = (guard, sp, rawId, userId, tenantId, ct) =>
                TKey.TryParse(rawId, CultureInfo.InvariantCulture, out var key)
                    ? guard.RequireOwnerAndTenantAsync(getQuery(sp), key, userId, tenantId, idSelector, ownerSelector, tenantSelector, ct)
                    : Task.FromResult(RequireOwnerResult.InvalidId);
        }

        if (!_executors.TryAdd(type, executor))
            throw new InvalidOperationException($"Ownership descriptor already registered for entity type: {type.FullName}.");

        if (tenantExecutor != null)
            _tenantExecutors.TryAdd(type, tenantExecutor);
    }

    public bool TryGetExecutor(Type entityType, out OwnershipCheckExecutor? executor)
    {
        return _executors.TryGetValue(entityType, out executor);
    }

    public bool TryGetTenantExecutor(Type entityType, out OwnershipTenantCheckExecutor? executor)
    {
        return _tenantExecutors.TryGetValue(entityType, out executor);
    }

    public OwnershipCheckExecutor GetExecutor(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        if (_executors.TryGetValue(entityType, out var executor))
            return executor;
        throw new OwnershipDescriptorNotRegisteredException(entityType, tenantAware: false);
    }

    public OwnershipTenantCheckExecutor GetTenantExecutor(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        if (_tenantExecutors.TryGetValue(entityType, out var executor))
            return executor;
        throw new OwnershipDescriptorNotRegisteredException(entityType, tenantAware: true);
    }
}
