using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OwnershipGuard;

/// <summary>
/// MVC filter that enforces ownership (and optional tenant) before the action runs.
/// </summary>
public sealed class RequireOwnershipActionFilter : IAsyncActionFilter
{
    private readonly string _routeParamName;
    private readonly Type _entityType;
    private readonly IAccessGuard _guard;
    private readonly IOwnershipDescriptorRegistry _registry;
    private readonly IOptions<OwnershipGuardOptions> _options;
    private readonly ILogger<RequireOwnershipActionFilter> _logger;

    public RequireOwnershipActionFilter(
        string routeParamName,
        Type entityType,
        IAccessGuard guard,
        IOwnershipDescriptorRegistry registry,
        IOptions<OwnershipGuardOptions> options,
        ILogger<RequireOwnershipActionFilter> logger)
    {
        _routeParamName = routeParamName ?? throw new ArgumentNullException(nameof(routeParamName));
        _entityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var options = _options.Value;

        IActionResult Error(int statusCode, string title)
        {
            if (options.UseProblemDetailsResponses)
                return Problem(title, statusCode);

            return statusCode switch
            {
                StatusCodes.Status400BadRequest => new BadRequestResult(),
                StatusCodes.Status404NotFound => new NotFoundResult(),
                _ => new StatusCodeResult(statusCode)
            };
        }

        if (!context.RouteData.Values.TryGetValue(_routeParamName, out var value))
        {
            context.Result = Error(StatusCodes.Status400BadRequest, "Missing resource id in route.");
            return;
        }
        var rawId = value?.ToString();
        if (string.IsNullOrWhiteSpace(rawId))
        {
            context.Result = Error(StatusCodes.Status400BadRequest, "Invalid resource id in route.");
            return;
        }

        var userId = httpContext.User.FindFirst(options.UserIdClaimType)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = Error(StatusCodes.Status401Unauthorized, "User not authenticated.");
            return;
        }

        var requiresTenant = _registry.TryGetTenantExecutor(_entityType, out _);
        string? tenantId = null;
        if (requiresTenant)
        {
            tenantId = httpContext.User.FindFirst(options.TenantIdClaimType)?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                context.Result = Error(StatusCodes.Status401Unauthorized, "Tenant not specified.");
                return;
            }
        }

        RequireOwnerResult result;
        try
        {
            result = requiresTenant
                ? await _guard.RequireOwnerAndTenantAsync(
                    _entityType,
                    rawId,
                    userId,
                    tenantId!,
                    httpContext.RequestServices,
                    httpContext.RequestAborted).ConfigureAwait(false)
                : await _guard.RequireOwnerAsync(
                    _entityType,
                    rawId,
                    userId,
                    httpContext.RequestServices,
                    httpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (OwnershipDescriptorNotRegisteredException ex)
        {
            _logger.LogError(ex, "Ownership descriptor not registered for entity type {EntityType}.", ex.EntityType.FullName);
            context.Result = Error(StatusCodes.Status500InternalServerError, "Ownership descriptor not registered for this entity type.");
            return;
        }

        if (result == RequireOwnerResult.Success)
        {
            await next().ConfigureAwait(false);
            return;
        }

        context.Result = result switch
        {
            RequireOwnerResult.InvalidId => Error(StatusCodes.Status400BadRequest, "Invalid resource id."),
            RequireOwnerResult.NotFound => Error(StatusCodes.Status404NotFound, "Not found."),
            _ => Error(StatusCodes.Status403Forbidden, "Forbidden.")
        };
    }

    private static ObjectResult Problem(string title, int statusCode)
    {
        var details = new ProblemDetails
        {
            Title = title,
            Status = statusCode
        };
        return new ObjectResult(details) { StatusCode = statusCode };
    }
}
