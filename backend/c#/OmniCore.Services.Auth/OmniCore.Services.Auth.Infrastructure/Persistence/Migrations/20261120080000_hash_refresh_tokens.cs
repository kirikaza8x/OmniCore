using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;

#nullable disable

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// ADR 0003, step 1: store refresh tokens as a SHA-256 hash instead of plain text (finding A6).
    /// Hand-written (no .NET SDK in the authoring sandbox); the model snapshot was edited to match and the integration
    /// test fails if they drift apart. Existing sessions are kept: the hash of every existing plain token is computed
    /// in SQL, with the same function the application uses (SHA-256 of the UTF-8 bytes, lowercase hex).
    /// The plain `token` and `replaced_by_token` columns are NOT dropped here, so this can be rolled back; a later
    /// migration drops them once this one has run in production.
    /// </summary>
    [DbContext(typeof(AuthDbContext))]
    [Migration("20261120080000_hash_refresh_tokens")]
    public partial class hash_refresh_tokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE auth.refresh_tokens ADD COLUMN token_hash character varying(64);
                ALTER TABLE auth.refresh_tokens ADD COLUMN replaced_by_token_hash character varying(64);

                UPDATE auth.refresh_tokens
                   SET token_hash = encode(sha256(convert_to(token, 'UTF8')), 'hex');

                UPDATE auth.refresh_tokens
                   SET replaced_by_token_hash = encode(sha256(convert_to(replaced_by_token, 'UTF8')), 'hex')
                 WHERE replaced_by_token IS NOT NULL;

                ALTER TABLE auth.refresh_tokens ALTER COLUMN token_hash SET NOT NULL;
                CREATE UNIQUE INDEX ix_refresh_tokens_token_hash ON auth.refresh_tokens (token_hash);

                -- The application no longer writes the plain value, so it must be allowed to be empty.
                DROP INDEX auth.ix_refresh_tokens_token;
                ALTER TABLE auth.refresh_tokens ALTER COLUMN token DROP NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rows created after the upgrade have no plain token and cannot be restored (that is the point of hashing):
            // they are deleted, which signs those users out. Rows from before the upgrade keep their plain token.
            migrationBuilder.Sql(@"
                DELETE FROM auth.refresh_tokens WHERE token IS NULL;
                ALTER TABLE auth.refresh_tokens ALTER COLUMN token SET NOT NULL;
                CREATE UNIQUE INDEX ix_refresh_tokens_token ON auth.refresh_tokens (token);
                DROP INDEX auth.ix_refresh_tokens_token_hash;
                ALTER TABLE auth.refresh_tokens DROP COLUMN replaced_by_token_hash;
                ALTER TABLE auth.refresh_tokens DROP COLUMN token_hash;
            ");
        }
    }
}
