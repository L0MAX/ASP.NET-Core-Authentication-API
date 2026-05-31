using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class RoleSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        var roleNames = new[] { RoleNames.User, RoleNames.Admin };

        foreach (var roleName in roleNames)
        {
            var exists = await context.Roles.AnyAsync(r => r.Name == roleName, cancellationToken);

            if (!exists)
            {
                await context.Roles.AddAsync(Role.Create(roleName), cancellationToken);
                logger.LogInformation("Seeded role {RoleName}", roleName);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
