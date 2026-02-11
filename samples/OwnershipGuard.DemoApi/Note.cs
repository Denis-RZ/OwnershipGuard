using OwnershipGuard;

namespace OwnershipGuard.DemoApi;

public sealed class Note : IOwnedResource
{
    public required Guid Id { get; set; }
    public required string OwnerId { get; set; }
    public required string Title { get; set; }
}

