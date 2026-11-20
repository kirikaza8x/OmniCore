using Microsoft.EntityFrameworkCore;
using Npgsql;
using OmniCore.Services.Auth.Infrastructure.Jobs;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using Testcontainers.PostgreSql;

namespace OmniCore.Services.Auth.IntegrationTests;

/// <summary>ADR 0003 step 3 (A10): the purge deletes rows expired more than the retention ago and nothing else.</summary>
public sealed class PurgeRefreshTokensTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Purge_DeletesOnlyRowsExpiredBeyondTheRetention()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), n => n.MigrationsHistoryTable("__EFMigrationsHistory", AuthDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()   // same model as the service uses; without it Migrate() sees "pending changes"
            .Options;
        await using var db = new AuthDbContext(options);
        await db.Database.MigrateAsync();

        await using (var connection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            // Accounts are not needed for this test, so the foreign key is dropped (same approach as the migration test).
            await using var insert = new NpgsqlCommand(@"
                ALTER TABLE auth.refresh_tokens DROP CONSTRAINT fk_refresh_tokens_accounts_account_id;
                INSERT INTO auth.refresh_tokens (id, account_id, token_hash, expires_at_utc, is_revoked, created_at_utc) VALUES
                  (gen_random_uuid(), gen_random_uuid(), 'old-expired',      now() - interval '40 days', false, now() - interval '47 days'),
                  (gen_random_uuid(), gen_random_uuid(), 'old-revoked',      now() - interval '31 days', true,  now() - interval '38 days'),
                  (gen_random_uuid(), gen_random_uuid(), 'recent-expired',   now() - interval '5 days',  false, now() - interval '12 days'),
                  (gen_random_uuid(), gen_random_uuid(), 'recent-revoked',   now() + interval '3 days',  true,  now() - interval '4 days'),
                  (gen_random_uuid(), gen_random_uuid(), 'active',           now() + interval '7 days',  false, now());", connection);
            await insert.ExecuteNonQueryAsync();
        }

        var deleted = await PurgeRefreshTokensJob.PurgeAsync(db, 30, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, deleted);
        var left = await db.Database.SqlQueryRaw<string>("SELECT token_hash AS \"Value\" FROM auth.refresh_tokens ORDER BY token_hash").ToListAsync();
        Assert.Equal(new[] { "active", "recent-expired", "recent-revoked" }, left);
    }
}
