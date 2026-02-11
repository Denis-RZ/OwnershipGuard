namespace OwnershipGuard;

/// <summary>
/// Marks an entity as belonging to a tenant. Use with tenant scoping to restrict access.
/// </summary>
public interface ITenantResource
{
    /// <summary>Identifier of the tenant this resource belongs to.</summary>
    string TenantId { get; }
}
