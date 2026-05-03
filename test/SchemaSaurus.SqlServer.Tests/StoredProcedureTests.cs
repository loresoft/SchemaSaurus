using System.Data;

using SchemaSaurus.SqlServer.Tests.Fixtures;

using ParameterDirection = SchemaSaurus.Metadata.ParameterDirection;

namespace SchemaSaurus.SqlServer.Tests;

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
    public async Task WhenReadingStoredProceduresThenStatusUpsertExists()
    {
        var model = await GetDatabaseModelAsync();

        model.StoredProcedures.Should().Contain(sp => sp.SchemaQualifiedName.Name == "StatusUpsert");
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenSchemaIsDbo()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.SchemaQualifiedName.Schema.Should().Be("dbo");
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenParametersExist()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        sp.Parameters.Should().HaveCount(3);
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenOffsetParameterIsInput()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        var offsetParam = sp.Parameters.First(p => p.Name == "@Offset");
        offsetParam.Direction.Should().Be(ParameterDirection.Input);
        offsetParam.DbType.Should().Be(DbType.Int32);
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenTotalParameterIsOutput()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.SchemaQualifiedName.Name == "StatusPaged");

        var totalParam = sp.Parameters.First(p => p.Name == "@Total");
        totalParam.Direction.Should().Be(ParameterDirection.Output);
        totalParam.DbType.Should().Be(DbType.Int64);
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
