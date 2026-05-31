using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasMany<Role>("_roles")
            .WithMany("_users")
            .UsingEntity<Dictionary<string, object>>(
                "UserRoles",
                j => j.HasOne<Role>()
                    .WithMany()
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("UserId", "RoleId");
                    j.HasIndex("RoleId");
                    j.ToTable("UserRoles");
                });

        builder.Navigation("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
