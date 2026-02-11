namespace OwnershipGuard;

/// <summary>
/// Thrown when an ownership descriptor for a given entity type was not registered in <see cref="IOwnershipDescriptorRegistry"/>.
/// </summary>
public sealed class OwnershipDescriptorNotRegisteredException : InvalidOperationException
{
    public Type EntityType { get; }
    public bool TenantAware { get; }

    public OwnershipDescriptorNotRegisteredException(Type entityType, bool tenantAware)
        : base(CreateMessage(entityType, tenantAware))
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        TenantAware = tenantAware;
    }

    public OwnershipDescriptorNotRegisteredException(Type entityType, bool tenantAware, Exception? innerException)
        : base(CreateMessage(entityType, tenantAware), innerException)
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        TenantAware = tenantAware;
    }

    private static string CreateMessage(Type entityType, bool tenantAware)
    {
        var typeName = entityType?.FullName ?? "<unknown>";
        return tenantAware
            ? $"Tenant ownership descriptor not registered for entity type: {typeName}."
            : $"Ownership descriptor not registered for entity type: {typeName}.";
    }
}

