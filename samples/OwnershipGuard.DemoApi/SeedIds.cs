namespace OwnershipGuard.DemoApi;

/// <summary>
/// Fixed GUIDs for seeded documents so tests can rely on them.
/// </summary>
public static class SeedIds
{
    public static readonly Guid Doc1Id = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Doc2Id = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Note1Id = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Note2Id = new("44444444-4444-4444-4444-444444444444");
}
