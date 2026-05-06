using System.Data;

using SchemaSaurus.PostgreSql.Tests.Fixtures;

using ParameterDirection = SchemaSaurus.Metadata.ParameterDirection;

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

        model.StoredProcedures.Should().Contain(sp => sp.QualifiedName.Name == "StatusPaged");
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenSchemaIsPublic()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.QualifiedName.Name == "StatusPaged");

        sp.QualifiedName.Schema.Should().Be("public");
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenParametersExist()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.QualifiedName.Name == "StatusPaged");

        sp.Parameters.Should().HaveCount(3);
        sp.Parameters.Should().Contain(p => p.Name == "Offset");
        sp.Parameters.Should().Contain(p => p.Name == "Limit");
        sp.Parameters.Should().Contain(p => p.Name == "Total");
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenOffsetParameterIsInput()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.QualifiedName.Name == "StatusPaged");

        var offsetParam = sp.Parameters.First(p => p.Name == "Offset");
        offsetParam.Direction.Should().Be(ParameterDirection.Input);
        offsetParam.DbType.Should().Be(DbType.Int32);
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenTotalParameterIsOutput()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.QualifiedName.Name == "StatusPaged");

        var totalParam = sp.Parameters.First(p => p.Name == "Total");
        totalParam.Direction.Should().Be(ParameterDirection.Output);
        totalParam.DbType.Should().Be(DbType.Int64);
    }

    [Fact]
    public async Task WhenReadingStoredProcedureWithDefinitionsThenDefinitionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.QualifiedName.Name == "StatusPaged");

        sp.Definition.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingStatusPagedThenDescriptionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var sp = model.StoredProcedures.First(sp => sp.QualifiedName.Name == "StatusPaged");

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
