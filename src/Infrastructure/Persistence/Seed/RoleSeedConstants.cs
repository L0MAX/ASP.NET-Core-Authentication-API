namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Fixed role identifiers used by EF Core HasData seeding and runtime seed fallback.
/// </summary>
public static class RoleSeedConstants
{
    public static readonly Guid AdminRoleId = Guid.Parse("a1a1a1a1-1111-4111-8111-111111111111");

    public static readonly Guid UserRoleId = Guid.Parse("b2b2b2b2-2222-4222-8222-222222222222");
}
