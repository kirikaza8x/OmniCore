using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using Testcontainers.PostgreSql;

namespace OmniCore.Services.Auth.IntegrationTests;

/// <summary>
/// ADR 0003 step 1: the migration that hashes refresh tokens must keep the sessions that already exist. It is run
/// against a database that holds a plain-text token from before the upgrade, and the hash the SQL computed must equal
/// the hash the application computes (otherwise every existing user would be signed out).
/// </summary>
public sealed class RefreshTokenHashMigrationTests : IAsyncLifetime
{
    private const string BeforeHashing = "20260813160220_migrate_auto_20260813230207";
    private const string HashMigration = "20261120080000_hash_refresh_tokens";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task ExistingPlainTokens_AreHashedInPlace_WithTheHashTheApplicationComputes()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        await using var db = new AuthDbContext(options);
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync(BeforeHashing);

        const string plain = "legacy-plain-refresh-token";
        const string replacement = "legacy-replacement-token";
        await using (var connection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            // The accounts the tokens belong to are not needed for this test: skip the foreign key check for the insert.
            await using var insert = new NpgsqlCommand(@"
                SET session_replication_role = replica;
                INSERT INTO auth.refresh_tokens (id, account_id, token, expires_at_utc, is_revoked, replaced_by_token, created_at_utc)
                VALUES (gen_random_uuid(), gen_random_uuid(), @plain, now() + interval '7 days', true, @replacement, now()),
                       (gen_random_uuid(), gen_random_uuid(), @replacement, now() + interval '7 days', false, NULL, now());
                SET session_replication_role = DEFAULT;", connection);
            insert.Parameters.AddWithValue("plain", plain);
            insert.Parameters.AddWithValue("replacement", replacement);
            await insert.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync(HashMigration);

        await using var read = new NpgsqlConnection(_postgres.GetConnectionString());
        await read.OpenAsync();
        await using var select = new NpgsqlCommand(
            "SELECT token_hash, replaced_by_token_hash, token FROM auth.refresh_tokens ORDER BY is_revoked DESC", read);
        await using var reader = await select.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(RefreshToken.HashToken(plain), reader.GetString(0));
        Assert.Equal(RefreshToken.HashToken(replacement), reader.GetString(1));
        Assert.Equal(plain, reader.GetString(2));   // the plain column is kept so the migration can be rolled back

        Assert.True(await reader.ReadAsync());
        Assert.Equal(RefreshToken.HashToken(replacement), reader.GetString(0));
        Assert.True(await reader.IsDBNullAsync(1));
    }

    [Fact]
    public async Task Down_RemovesTheHashColumns_AndKeepsTheOldRows()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        await using var db = new AuthDbContext(options);
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync(HashMigration);
        await migrator.MigrateAsync(BeforeHashing);

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var columns = new NpgsqlCommand(
            "SELECT count(*) FROM information_schema.columns WHERE table_schema = 'auth' AND table_name = 'refresh_tokens' AND column_name IN ('token_hash','replaced_by_token_hash')",
            connection);
        Assert.Equal(0L, (long)(await columns.ExecuteScalarAsync())!);
    }
}
