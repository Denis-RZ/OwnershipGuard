using System.Security.Claims;

namespace OwnershipGuard;

/// <summary>
/// Configuration options for OwnershipGuard.
/// </summary>
public class OwnershipGuardOptions
{
    /// <summary>
    /// Claim type used to read the current user id from <see cref="ClaimsPrincipal"/>.
    /// Defaults to <see cref="ClaimTypes.NameIdentifier"/>.
    /// </summary>
    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>
    /// Claim type used to read the current tenant id from <see cref="ClaimsPrincipal"/> when tenant checks are enabled.
    /// Defaults to <c>tenant_id</c>.
    /// </summary>
    public string TenantIdClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// When true, filters return <see cref="Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult"/> / <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>
    /// responses for 4xx/5xx errors. When false, filters return plain status code results (e.g. 404, 403).
    /// Defaults to true.
    /// </summary>
    public bool UseProblemDetailsResponses { get; set; } = true;

    /// <summary>
    /// When true, return 404 (Not Found) instead of 403 (Forbidden) when the user is not the owner.
    /// Hides existence of the resource from unauthorized users. Default is false (return 403).
    /// </summary>
    public bool HideExistenceWhenForbidden { get; set; }
}
