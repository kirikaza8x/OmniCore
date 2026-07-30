namespace OmniCore.Shared.Infrastructure.Data.Seeds;

public interface IDataSeeder
{
    /// <summary>
    /// Execution order priority. Seeders execute in ascending order (lowest numbers run first).
    /// Defaults to 0.
    /// </summary>
    int Order => 0;

    Task SeedAllAsync();
}

public interface IDataSeeder<T> : IDataSeeder where T : class
{
}