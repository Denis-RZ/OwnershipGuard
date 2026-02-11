using OwnershipGuard;

namespace OwnershipGuard.DemoApi;

public sealed class Document : IOwnedResource, ITenantResource
{
    public required Guid Id { get; set; }
    public required string OwnerId { get; set; }
    public required string TenantId { get; set; }
    public required string Title { get; set; }
    public string Content { get; set; } = string.Empty;
}
