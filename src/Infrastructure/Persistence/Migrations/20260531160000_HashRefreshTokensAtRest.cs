using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class HashRefreshTokensAtRest : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM [RefreshTokens];");

        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens");

        migrationBuilder.RenameColumn(
            name: "Token",
            table: "RefreshTokens",
            newName: "TokenHash");

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "RefreshTokens",
            type: "rowversion",
            rowVersion: true,
            nullable: false,
            defaultValue: new byte[8]);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "RefreshTokens");

        migrationBuilder.RenameColumn(
            name: "TokenHash",
            table: "RefreshTokens",
            newName: "Token");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens",
            column: "Token",
            unique: true);
    }
}
