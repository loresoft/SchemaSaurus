using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

public class DatabaseMetadataTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenProviderIsOracle()
    {
        var model = await GetDatabaseModelAsync();

        model.Provider.Should().Be("Oracle");
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
    public async Task WhenReadingDatabaseThenCompatibilityLevelIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.CompatibilityLevel.Should().NotBeNullOrWhiteSpace();
    }
}
