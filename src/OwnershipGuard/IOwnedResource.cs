namespace OwnershipGuard;

/// <summary>
/// Marks an entity as owned by a user. Use with ownership checks to prevent IDOR.
/// </summary>
public interface IOwnedResource
{
    /// <summary>Identifier of the user who owns this resource.</summary>
    string OwnerId { get; }
}
