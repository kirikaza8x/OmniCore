using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;

#nullable disable

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// ADR 0001 / plan 0007 step 2a: tenants and memberships. Hand-written (no .NET SDK in the authoring sandbox); the
    /// model snapshot was edited to match and the integration test fails if they drift apart.
    /// Creates the default tenant with its fixed id and gives every existing account a membership of it
    /// (Admin -> TenantAdmin, everyone else -> Member; SuperAdmin is a platform role and stays on the account).
    /// Nothing reads these tables yet: tokens and handlers change in step 2b.
    /// </summary>
    [DbContext(typeof(AuthDbContext))]
    [Migration("20261124120000_add_tenants")]
    public partial class add_tenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE auth.tenants (
                    id uuid NOT NULL,
                    slug character varying(63) NOT NULL,
                    name character varying(100) NOT NULL,
                    is_active boolean NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_tenants PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX ix_tenants_slug ON auth.tenants (slug);

                CREATE TABLE auth.tenant_memberships (
                    tenant_id uuid NOT NULL,
                    account_id uuid NOT NULL,
                    role character varying(30) NOT NULL,
                    joined_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_tenant_memberships PRIMARY KEY (tenant_id, account_id),
                    CONSTRAINT fk_tenant_memberships_accounts_account_id FOREIGN KEY (account_id)
                        REFERENCES auth.accounts (id) ON DELETE CASCADE,
                    CONSTRAINT fk_tenant_memberships_tenants_tenant_id FOREIGN KEY (tenant_id)
                        REFERENCES auth.tenants (id) ON DELETE CASCADE
                );
                CREATE INDEX ix_tenant_memberships_account_id ON auth.tenant_memberships (account_id);

                INSERT INTO auth.tenants (id, slug, name, is_active, created_at_utc)
                VALUES ('7e4a0001-0000-4000-8000-000000000001', 'default', 'Default', true, now());

                INSERT INTO auth.tenant_memberships (tenant_id, account_id, role, joined_at_utc)
                SELECT '7e4a0001-0000-4000-8000-000000000001', a.id,
                       CASE WHEN EXISTS (
                                SELECT 1 FROM auth.account_roles ar
                                JOIN auth.roles r ON r.id = ar.role_id
                                WHERE ar.account_id = a.id AND r.name = 'Admin')
                            THEN 'TenantAdmin' ELSE 'Member' END,
                       now()
                FROM auth.accounts a;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE auth.tenant_memberships;
                DROP TABLE auth.tenants;
            ");
        }
    }
}
