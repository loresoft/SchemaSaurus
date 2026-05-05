using SchemaSaurus.MySql.Tests.Fixtures;

namespace SchemaSaurus.MySql.Tests;

public class DatabaseMetadataTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenProviderIsMySql()
    {
        var model = await GetDatabaseModelAsync();

        model.Provider.Should().Be("MySQL");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenDefaultSchemaNameIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.DefaultSchemaName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingDatabaseThenServerVersionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.ServerVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingDatabaseThenCollationIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.Collation.Should().NotBeNullOrWhiteSpace();
    }
}
