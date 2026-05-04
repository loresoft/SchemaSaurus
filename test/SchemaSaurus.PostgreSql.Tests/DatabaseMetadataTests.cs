using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class DatabaseMetadataTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenDatabaseNameIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.DatabaseName.Should().Be("SchemaSaurus");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenProviderIsPostgreSql()
    {
        var model = await GetDatabaseModelAsync();

        model.Provider.Should().Be("PostgreSQL");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenDefaultSchemaNameIsPublic()
    {
        var model = await GetDatabaseModelAsync();

        model.DefaultSchemaName.Should().Be("public");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenCollationIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.Collation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingDatabaseThenServerVersionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.ServerVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingDatabaseThenEngineEditionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.EngineEdition.Should().Be("PostgreSQL");
    }
}
