using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class DatabaseMetadataTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenDatabaseNameIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.DatabaseName.Should().Be("main");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenProviderIsSQLite()
    {
        var model = await GetDatabaseModelAsync();

        model.Provider.Should().Be("SQLite");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenDefaultSchemaNameIsNull()
    {
        var model = await GetDatabaseModelAsync();

        model.DefaultSchemaName.Should().BeNull();
    }

    [Fact]
    public async Task WhenReadingDatabaseThenCollationIsEncoding()
    {
        var model = await GetDatabaseModelAsync();

        model.Collation.Should().Be("UTF-8");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenServerVersionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.ServerVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingDatabaseThenEditionIsSQLite()
    {
        var model = await GetDatabaseModelAsync();

        model.Edition.Should().Be("SQLite");
    }

    [Fact]
    public async Task WhenReadingDatabaseThenPragmaAnnotationsArePopulated()
    {
        var model = await GetDatabaseModelAsync();

        model.Annotations.Should().ContainKey(SqliteAnnotations.ForeignKeys);
        model.Annotations.Should().ContainKey(SqliteAnnotations.JournalMode);
        model.Annotations.Should().ContainKey(SqliteAnnotations.PageSize);
    }
}
