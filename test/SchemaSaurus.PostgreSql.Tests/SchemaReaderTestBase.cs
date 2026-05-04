using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.PostgreSql.Tests.Fixtures;
using SchemaSaurus.PostgreSql;

namespace SchemaSaurus.PostgreSql.Tests;

/// <summary>
/// Base class for schema reader integration tests. Provides a cached
/// <see cref="DatabaseModel"/> snapshot read once per test class lifetime.
/// </summary>
public abstract class SchemaReaderTestBase : DatabaseTestBase
{
    private static DatabaseModel? s_cachedModel;
    private static readonly SemaphoreSlim s_lock = new(1, 1);

    protected SchemaReaderTestBase(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    /// <summary>
    /// Returns the <see cref="DatabaseModel"/> for the test database, reading it
    /// from the database on the first call and caching for subsequent calls.
    /// </summary>
    protected async Task<DatabaseModel> GetDatabaseModelAsync(SchemaReaderOptions? options = null)
    {
        // When custom options are provided, always read fresh
        if (options is not null)
            return await ReadSchemaAsync(options);

        if (s_cachedModel is not null)
            return s_cachedModel;

        await s_lock.WaitAsync();
        try
        {
            s_cachedModel ??= await ReadSchemaAsync(new SchemaReaderOptions());
            return s_cachedModel;
        }
        finally
        {
            s_lock.Release();
        }
    }

    private async Task<DatabaseModel> ReadSchemaAsync(SchemaReaderOptions options)
    {
        var configuration = Services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("SchemaSaurus")!;

        var reader = new PostgreSqlSchemaReader();
        return await reader.ReadAsync(connectionString, options);
    }
}
