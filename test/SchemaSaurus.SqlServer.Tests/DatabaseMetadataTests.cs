using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

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
    public async Task WhenReadingDatabaseThenProviderIsSqlServer()
    {
        var model = await GetDatabaseModelAsync();

        model.Provider.Should().Be("SqlServer");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenDefaultSchemaNameIsDbo()
    {
        var model = await GetDatabaseModelAsync();

        model.DefaultSchemaName.Should().Be("dbo");
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
}
