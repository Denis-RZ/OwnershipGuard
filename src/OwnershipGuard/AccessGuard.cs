using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace OwnershipGuard;

/// <inheritdoc />
public sealed class AccessGuard : IAccessGuard
{
    private readonly OwnershipGuardOptions _options;
    private readonly IOwnershipDescriptorRegistry _registry;

    public AccessGuard(IOptions<OwnershipGuardOptions> options, IOwnershipDescriptorRegistry registry)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public async Task<bool> IsOwnerAsync<T>(
        IQueryable<T> query,
        string resourceId,
        string userId,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        CancellationToken cancellationToken = default)
    {
        var result = await RequireOwnerAsync(query, resourceId, userId, idSelector, ownerSelector, cancellationToken)
            .ConfigureAwait(false);
        return result == RequireOwnerResult.Success;
    }

    /// <inheritdoc />
    public async Task<RequireOwnerResult> RequireOwnerAsync<T>(
        IQueryable<T> query,
        string resourceId,
        string userId,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(ownerSelector);
        ArgumentException.ThrowIfNullOrEmpty(resourceId);
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var idPredicate = BuildIdPredicate(idSelector, resourceId);
        var ownedSelector = BuildEqualsSelector(ownerSelector, userId);
        var owned = await query.Where(idPredicate).Select(ownedSelector).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (owned is null)
            return RequireOwnerResult.NotFound;
        if (owned.Value)
            return RequireOwnerResult.Success;

        return _options.HideExistenceWhenForbidden ? RequireOwnerResult.NotFound : RequireOwnerResult.Forbidden;
    }

    /// <inheritdoc />
    public async Task<RequireOwnerResult> RequireOwnerAsync<T, TKey>(
        IQueryable<T> query,
        TKey resourceId,
        string userId,
        Expression<Func<T, TKey>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        CancellationToken cancellationToken = default)
        where TKey : IParsable<TKey>
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(ownerSelector);
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var idPredicate = BuildIdPredicate(idSelector, resourceId);
        var ownedSelector = BuildEqualsSelector(ownerSelector, userId);
        var owned = await query.Where(idPredicate).Select(ownedSelector).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (owned is null)
            return RequireOwnerResult.NotFound;
        if (owned.Value)
            return RequireOwnerResult.Success;

        return _options.HideExistenceWhenForbidden ? RequireOwnerResult.NotFound : RequireOwnerResult.Forbidden;
    }

    /// <inheritdoc />
    public async Task<RequireOwnerResult> RequireOwnerAndTenantAsync<T>(
        IQueryable<T> query,
        string resourceId,
        string userId,
        string tenantId,
        Expression<Func<T, string>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>> tenantSelector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(ownerSelector);
        ArgumentNullException.ThrowIfNull(tenantSelector);
        ArgumentException.ThrowIfNullOrEmpty(resourceId);
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var idPredicate = BuildIdPredicate(idSelector, resourceId);
        var allowedSelector = BuildAndEqualsSelector(ownerSelector, userId, tenantSelector, tenantId);
        var allowed = await query.Where(idPredicate).Select(allowedSelector).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (allowed is null)
            return RequireOwnerResult.NotFound;
        if (allowed.Value)
            return RequireOwnerResult.Success;

        return _options.HideExistenceWhenForbidden ? RequireOwnerResult.NotFound : RequireOwnerResult.Forbidden;
    }

    /// <inheritdoc />
    public async Task<RequireOwnerResult> RequireOwnerAndTenantAsync<T, TKey>(
        IQueryable<T> query,
        TKey resourceId,
        string userId,
        string tenantId,
        Expression<Func<T, TKey>> idSelector,
        Expression<Func<T, string>> ownerSelector,
        Expression<Func<T, string>> tenantSelector,
        CancellationToken cancellationToken = default)
        where TKey : IParsable<TKey>
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(ownerSelector);
        ArgumentNullException.ThrowIfNull(tenantSelector);
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var idPredicate = BuildIdPredicate(idSelector, resourceId);
        var allowedSelector = BuildAndEqualsSelector(ownerSelector, userId, tenantSelector, tenantId);
        var allowed = await query.Where(idPredicate).Select(allowedSelector).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (allowed is null)
            return RequireOwnerResult.NotFound;
        if (allowed.Value)
            return RequireOwnerResult.Success;

        return _options.HideExistenceWhenForbidden ? RequireOwnerResult.NotFound : RequireOwnerResult.Forbidden;
    }

    /// <inheritdoc />
    public async Task<RequireOwnerResult> RequireOwnerAsync(
        Type entityType,
        string resourceId,
        string userId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentException.ThrowIfNullOrEmpty(resourceId);
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var executor = _registry.GetExecutor(entityType);
        return await executor(this, serviceProvider, resourceId, userId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RequireOwnerResult> RequireOwnerAndTenantAsync(
        Type entityType,
        string resourceId,
        string userId,
        string tenantId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentException.ThrowIfNullOrEmpty(resourceId);
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var executor = _registry.GetTenantExecutor(entityType);
        return await executor(this, serviceProvider, resourceId, userId, tenantId, cancellationToken).ConfigureAwait(false);
    }

    private static Expression<Func<T, bool>> BuildIdPredicate<T>(Expression<Func<T, string>> idSelector, string resourceId)
    {
        var param = idSelector.Parameters[0];
        var idBody = ParameterReplacer.Replace(idSelector.Body, idSelector.Parameters[0], param);
        var equals = Expression.Equal(idBody, Expression.Constant(resourceId, typeof(string)));
        return Expression.Lambda<Func<T, bool>>(equals, param);
    }

    private static Expression<Func<T, bool?>> BuildEqualsSelector<T>(
        Expression<Func<T, string>> selector,
        string value)
    {
        var param = selector.Parameters[0];
        var body = ParameterReplacer.Replace(selector.Body, selector.Parameters[0], param);
        var equals = Expression.Equal(body, Expression.Constant(value, typeof(string)));
        return Expression.Lambda<Func<T, bool?>>(Expression.Convert(equals, typeof(bool?)), param);
    }

    private static Expression<Func<T, bool>> BuildIdPredicate<T, TKey>(Expression<Func<T, TKey>> idSelector, TKey resourceId)
        where TKey : IParsable<TKey>
    {
        var param = idSelector.Parameters[0];
        var idBody = ParameterReplacer.Replace(idSelector.Body, idSelector.Parameters[0], param);
        var equals = Expression.Equal(idBody, Expression.Constant(resourceId, typeof(TKey)));
        return Expression.Lambda<Func<T, bool>>(equals, param);
    }

    private static Expression<Func<T, bool?>> BuildAndEqualsSelector<T>(
        Expression<Func<T, string>> selector1,
        string value1,
        Expression<Func<T, string>> selector2,
        string value2)
    {
        var param = selector1.Parameters[0];
        var body1 = ParameterReplacer.Replace(selector1.Body, selector1.Parameters[0], param);
        var body2 = ParameterReplacer.Replace(selector2.Body, selector2.Parameters[0], param);
        var equals1 = Expression.Equal(body1, Expression.Constant(value1, typeof(string)));
        var equals2 = Expression.Equal(body2, Expression.Constant(value2, typeof(string)));
        var and = Expression.AndAlso(equals1, equals2);
        return Expression.Lambda<Func<T, bool?>>(Expression.Convert(and, typeof(bool?)), param);
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        private ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        internal static Expression Replace(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
        {
            return new ParameterReplacer(oldParam, newParam).Visit(expression);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParam ? _newParam : base.VisitParameter(node);
        }
    }
}
