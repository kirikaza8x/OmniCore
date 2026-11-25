using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OmniCore.Services.User.Infrastructure.Persistence.Contexts;

#nullable disable

namespace OmniCore.Services.User.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// ADR 0001 / plan 0007 step 3: per-tenant profiles. Hand-written (no .NET SDK in the authoring sandbox); the model
    /// snapshot was edited to match and the integration test fails if they drift apart.
    /// New table only: existing profiles are untouched and get a tenant profile lazily on the first PUT.
    /// </summary>
    [DbContext(typeof(UserDbContext))]
    [Migration("20261125090000_add_tenant_profiles")]
    public partial class add_tenant_profiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE users.tenant_profiles (
                    id uuid NOT NULL,
                    tenant_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    gamer_tag character varying(50) NOT NULL,
                    custom_avatar_url character varying(2048),
                    joined_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_tenant_profiles PRIMARY KEY (id),
                    CONSTRAINT fk_tenant_profiles_user_profiles_user_id FOREIGN KEY (user_id)
                        REFERENCES users.user_profiles (id) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX ix_tenant_profiles_user_id_tenant_id ON users.tenant_profiles (user_id, tenant_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE users.tenant_profiles;");
        }
    }
}
