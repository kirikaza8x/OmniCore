using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class migrate_auto_20260804203258 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_accounts_email",
                schema: "auth",
                table: "accounts");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "auth",
                table: "accounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<string>(
                name: "username",
                schema: "auth",
                table: "accounts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_accounts_email",
                schema: "auth",
                table: "accounts",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_accounts_username",
                schema: "auth",
                table: "accounts",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_accounts_email",
                schema: "auth",
                table: "accounts");

            migrationBuilder.DropIndex(
                name: "ix_accounts_username",
                schema: "auth",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "username",
                schema: "auth",
                table: "accounts");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "auth",
                table: "accounts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_email",
                schema: "auth",
                table: "accounts",
                column: "email",
                unique: true);
        }
    }
}
