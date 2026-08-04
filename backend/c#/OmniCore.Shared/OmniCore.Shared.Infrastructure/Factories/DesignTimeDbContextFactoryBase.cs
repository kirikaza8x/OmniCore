namespace OmniCore.Shared.Infrastructure.Persistence.Factories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using OmniCore.Shared.Infrastructure.Configs.Database;

public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// The directory name of the API startup project (e.g., "OmniCore.Services.Auth.Api").
    /// </summary>
    protected abstract string ApiProjectName { get; }

    /// <summary>
    /// Database schema name for the migrations history table (defaults to "public").
    /// </summary>
    protected virtual string SchemaName => "public";

    public TContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.Combine(basePath, $"../{ApiProjectName}");

        if (!File.Exists(Path.Combine(basePath, "appsettings.json")) && Directory.Exists(apiPath))
        {
            basePath = apiPath;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var dbConfig = new DatabaseConfig();
        configuration.GetSection(dbConfig.SectionName).Bind(dbConfig);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        optionsBuilder.UseNpgsql(dbConfig.ConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", SchemaName);

            if (dbConfig.MaxRetryCount > 0)
                npgsqlOptions.EnableRetryOnFailure(dbConfig.MaxRetryCount);

            if (dbConfig.CommandTimeout > 0)
                npgsqlOptions.CommandTimeout(dbConfig.CommandTimeout);
        })
        .UseSnakeCaseNamingConvention();

        if (dbConfig.EnableDetailedErrors)
            optionsBuilder.EnableDetailedErrors();

        if (dbConfig.EnableSensitiveDataLogging)
            optionsBuilder.EnableSensitiveDataLogging();

        return CreateDbContextInstance(optionsBuilder.Options);
    }

    /// <summary>
    /// Instantiates the specific DbContext using the configured options.
    /// Overridable if your DbContext has custom parameters.
    /// </summary>
    protected virtual TContext CreateDbContextInstance(DbContextOptions<TContext> options)
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}