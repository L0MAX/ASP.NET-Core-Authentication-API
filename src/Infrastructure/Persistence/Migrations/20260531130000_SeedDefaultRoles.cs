using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Admin')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('a1a1a1a1-1111-4111-8111-111111111111', N'Admin');

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'User')
                    INSERT INTO [Roles] ([Id], [Name]) VALUES ('b2b2b2b2-2222-4222-8222-222222222222', N'User');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-1111-4111-8111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("b2b2b2b2-2222-4222-8222-222222222222"));
        }
    }
}
