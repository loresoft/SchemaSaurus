using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class StoredProcedureTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingStoredProceduresThenProceduresAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        model.StoredProcedures.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingStoredProceduresThenStatusPagedExists()
    {
        var model = await GetDatabaseModelAsync();

        model.StoredProcedures.Should().Contain(sp => sp.SchemaQualifiedName.Name == "StatusPaged");
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenSchemaIsPublic()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.SchemaQualifiedName.Schema.Should().Be("public");
    }

    [Fact]
    public async Task WhenReadingStoredProcedureWithDefinitionsThenDefinitionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Definition.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenDescriptionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Description.Should().Be("Reads a page of statuses.");
    }

    [Fact]
    public async Task WhenExcludingStoredProceduresThenNoProceduresReturned()
    {
        var options = new Metadata.Provider.SchemaReaderOptions
        {
            IncludeStoredProcedures = false
        };

        var model = await GetDatabaseModelAsync(options);

        model.StoredProcedures.Should().BeEmpty();
    }
}
