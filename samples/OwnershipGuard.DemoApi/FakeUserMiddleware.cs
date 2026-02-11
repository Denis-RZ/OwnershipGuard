using System.Security.Claims;

namespace OwnershipGuard.DemoApi;

/// <summary>
/// Demo middleware: sets the current user from "X-User" header or defaults to "user1".
/// For production, use real authentication.
/// </summary>
public sealed class FakeUserMiddleware
{
    private const string DefaultUserId = "user1";
    private const string DefaultTenantIdForUser1 = "tenant1";
    private const string DefaultTenantIdForUser2 = "tenant2";
    private const string TenantClaimType = "tenant_id";
    private readonly RequestDelegate _next;

    public FakeUserMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var userId = context.Request.Headers["X-User"].FirstOrDefault() ?? DefaultUserId;
        var tenantId = context.Request.Headers["X-Tenant"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantId))
            tenantId = string.Equals(userId, "user2", StringComparison.OrdinalIgnoreCase)
                ? DefaultTenantIdForUser2
                : DefaultTenantIdForUser1;
        var identity = new ClaimsIdentity("Fake");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        identity.AddClaim(new Claim(TenantClaimType, tenantId));
        context.User = new ClaimsPrincipal(identity);
        return _next(context);
    }
}
