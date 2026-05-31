using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations and seeds default roles (Admin, User).
    /// </summary>
    public static async Task ApplyMigrationsAndSeedAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync(cancellationToken);

        await RoleSeeder.SeedAsync(context, logger, cancellationToken);
    }
}

public static class RoleSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var seeded = false;

        seeded |= await SeedRoleAsync(context, RoleSeedConstants.AdminRoleId, RoleNames.Admin, cancellationToken);
        seeded |= await SeedRoleAsync(context, RoleSeedConstants.UserRoleId, RoleNames.User, cancellationToken);

        if (seeded)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Default roles seeded successfully.");
        }
    }

    private static async Task<bool> SeedRoleAsync(
        ApplicationDbContext context,
        Guid id,
        string name,
        CancellationToken cancellationToken)
    {
        var exists = await context.Roles.AnyAsync(r => r.Name == name, cancellationToken);

        if (exists)
        {
            return false;
        }

        await context.Roles.AddAsync(Role.CreateForSeed(id, name), cancellationToken);
        return true;
    }
}
