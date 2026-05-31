using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.Navigation("_users")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(
            new { Id = RoleSeedConstants.AdminRoleId, Name = RoleNames.Admin },
            new { Id = RoleSeedConstants.UserRoleId, Name = RoleNames.User });
    }
}
