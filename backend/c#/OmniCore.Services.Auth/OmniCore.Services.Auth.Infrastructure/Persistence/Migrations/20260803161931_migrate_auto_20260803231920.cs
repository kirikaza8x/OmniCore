using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class migrate_auto_20260803231920 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_logins",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_logins", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_logins_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mfa_methods",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mfa_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_mfa_methods_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mfa_recovery_codes",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mfa_recovery_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_mfa_recovery_codes_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    replaced_by_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_audit_logs",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_audit_logs_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "security_tokens",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    token_type = table.Column<int>(type: "integer", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_tokens_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_roles",
                schema: "auth",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_roles", x => new { x.account_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_account_roles_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_account_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "auth",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    last_active_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "auth",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_sessions_refresh_tokens_refresh_token_id",
                        column: x => x.refresh_token_id,
                        principalSchema: "auth",
                        principalTable: "refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_roles_role_id",
                schema: "auth",
                table: "account_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounts_email",
                schema: "auth",
                table: "accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_account_id",
                schema: "auth",
                table: "external_logins",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_provider_provider_key",
                schema: "auth",
                table: "external_logins",
                columns: new[] { "provider", "provider_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mfa_methods_account_id",
                schema: "auth",
                table: "mfa_methods",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_mfa_recovery_codes_account_id",
                schema: "auth",
                table: "mfa_recovery_codes",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_account_id",
                schema: "auth",
                table: "refresh_tokens",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                schema: "auth",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                schema: "auth",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_account_id",
                schema: "auth",
                table: "security_audit_logs",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_tokens_account_id",
                schema: "auth",
                table: "security_tokens",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_tokens_code_hash",
                schema: "auth",
                table: "security_tokens",
                column: "code_hash");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_account_id",
                schema: "auth",
                table: "user_sessions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_refresh_token_id",
                schema: "auth",
                table: "user_sessions",
                column: "refresh_token_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "external_logins",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "mfa_methods",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "mfa_recovery_codes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "security_audit_logs",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "security_tokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "auth");
        }
    }
}
