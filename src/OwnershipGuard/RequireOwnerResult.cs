namespace OwnershipGuard;

/// <summary>
/// Result of an ownership check for <see cref="IAccessGuard.RequireOwnerAsync"/>.
/// </summary>
public enum RequireOwnerResult
{
    /// <summary>User is the owner; allow access.</summary>
    Success,

    /// <summary>Resource exists but user is not the owner; return 403 Forbidden (or 404 if hide mode).</summary>
    Forbidden,

    /// <summary>Resource does not exist; return 404 Not Found (always).</summary>
    NotFound,

    /// <summary>Resource id is invalid (e.g. not a valid Guid); return 400 Bad Request.</summary>
    InvalidId
}
