using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

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
    public async Task WhenReadingStatusPagedThenSchemaIsDefaultSchema()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.SchemaQualifiedName.Schema.Should().Be(model.DefaultSchemaName);
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenParametersExist()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Parameters.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenOffsetParameterIsInput()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Parameters.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenTotalParameterIsOutput()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Parameters.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingStoredProcedureWithDefinitionsThenDefinitionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Definition.Should().NotBeNullOrWhiteSpace();
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

    [Fact]
    public async Task WhenReadingParametersThenOrdinalsAreSequential()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Parameters.Should().AllSatisfy(p => p.Ordinal.Should().BeGreaterThan(0));
        sp.Parameters.Select(p => p.Ordinal).Should().BeInAscendingOrder();
    }
}
