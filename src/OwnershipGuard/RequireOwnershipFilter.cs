using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OwnershipGuard;

/// <summary>
/// Endpoint filter that enforces ownership before the endpoint runs. Short-circuits with 403 or 404 when the user is not the owner.
/// </summary>
public sealed class RequireOwnershipFilter : IEndpointFilter
{
    private readonly string _routeParamName;
    private readonly Type _entityType;

    /// <summary>
    /// Creates a filter that requires the current user to own the resource identified by the route parameter.
    /// </summary>
    /// <param name="routeParamName">Route parameter name containing the resource id (e.g. "id").</param>
    /// <param name="entityType">Entity type registered with <see cref="IOwnershipDescriptorRegistry"/> (e.g. typeof(Document)).</param>
    public RequireOwnershipFilter(string routeParamName, Type entityType)
    {
        _routeParamName = routeParamName ?? throw new ArgumentNullException(nameof(routeParamName));
        _entityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var options = httpContext.RequestServices.GetRequiredService<IOptions<OwnershipGuardOptions>>().Value;

        object Error(int statusCode, string title)
        {
            if (options.UseProblemDetailsResponses)
                return Results.Problem(title, statusCode: statusCode);

            return statusCode switch
            {
                StatusCodes.Status400BadRequest => Results.BadRequest(),
                StatusCodes.Status404NotFound => Results.NotFound(),
                _ => Results.StatusCode(statusCode)
            };
        }

        var routeValues = httpContext.Request.RouteValues;
        if (!routeValues.TryGetValue(_routeParamName, out var value))
        {
            return Error(StatusCodes.Status400BadRequest, "Missing resource id in route.");
        }
        var rawId = value?.ToString();
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return Error(StatusCodes.Status400BadRequest, "Invalid resource id in route.");
        }

        var claim = httpContext.User.FindFirst(options.UserIdClaimType);
        var userId = claim?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Error(StatusCodes.Status401Unauthorized, "User not authenticated.");
        }

        var guard = httpContext.RequestServices.GetRequiredService<IAccessGuard>();
        var registry = httpContext.RequestServices.GetRequiredService<IOwnershipDescriptorRegistry>();

        var requiresTenant = registry.TryGetTenantExecutor(_entityType, out _);
        string? tenantId = null;
        if (requiresTenant)
        {
            var tenantClaim = httpContext.User.FindFirst(options.TenantIdClaimType);
            tenantId = tenantClaim?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Error(StatusCodes.Status401Unauthorized, "Tenant not specified.");
            }
        }

        RequireOwnerResult result;
        try
        {
            result = requiresTenant
                ? await guard.RequireOwnerAndTenantAsync(
                    _entityType,
                    rawId,
                    userId,
                    tenantId!,
                    httpContext.RequestServices,
                    httpContext.RequestAborted).ConfigureAwait(false)
                : await guard.RequireOwnerAsync(
                    _entityType,
                    rawId,
                    userId,
                    httpContext.RequestServices,
                    httpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (OwnershipDescriptorNotRegisteredException ex)
        {
            httpContext.RequestServices
                .GetService<ILogger<RequireOwnershipFilter>>()
                ?.LogError(ex, "Ownership descriptor not registered for entity type {EntityType}.", ex.EntityType.FullName);

            return Error(StatusCodes.Status500InternalServerError, "Ownership descriptor not registered for this entity type.");
        }

        if (result == RequireOwnerResult.Success)
            return await next(context).ConfigureAwait(false);

        if (result == RequireOwnerResult.InvalidId)
        {
            return Error(StatusCodes.Status400BadRequest, "Invalid resource id.");
        }
        if (result == RequireOwnerResult.NotFound)
        {
            return Error(StatusCodes.Status404NotFound, "Not found.");
        }
        // Forbidden (or hide-existence treated as NotFound above is already handled - NotFound is 404)
        return Error(StatusCodes.Status403Forbidden, "Forbidden.");
    }
}
