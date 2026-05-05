using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.MySql.Tests.Fixtures;

namespace SchemaSaurus.MySql.Tests;

public abstract class SchemaReaderTestBase : DatabaseTestBase
{
    private static DatabaseModel? s_cachedModel;
    private static readonly SemaphoreSlim s_lock = new(1, 1);

    protected SchemaReaderTestBase(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    protected async Task<DatabaseModel> GetDatabaseModelAsync(SchemaReaderOptions? options = null)
    {
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
        var connectionString = configuration.GetConnectionString("SchemaSaurus");
        connectionString.Should().NotBeNullOrWhiteSpace();

        var reader = new MySqlSchemaReader();
        return await reader.ReadAsync(connectionString, options);
    }
}
