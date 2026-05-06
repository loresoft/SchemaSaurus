using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class UnsupportedObjectTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingSequencesThenNoSequencesReturned()
    {
        var model = await GetDatabaseModelAsync();

        model.Sequences.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingStoredProceduresThenNoStoredProceduresReturned()
    {
        var model = await GetDatabaseModelAsync();

        model.StoredProcedures.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingScalarFunctionsThenNoScalarFunctionsReturned()
    {
        var model = await GetDatabaseModelAsync();

        model.ScalarFunctions.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingTableValuedFunctionsThenNoTableValuedFunctionsReturned()
    {
        var model = await GetDatabaseModelAsync();

        model.TableValuedFunctions.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingUserDefinedTypesThenNoUserDefinedTypesReturned()
    {
        var model = await GetDatabaseModelAsync();

        model.UserDefinedTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenExcludingViewsThenNoViewsReturned()
    {
        var options = new SchemaReaderOptions
        {
            IncludeViews = false
        };

        var model = await GetDatabaseModelAsync(options);

        model.Views.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenReadingViewsThenViewIsReturned()
    {
        var model = await GetDatabaseModelAsync();

        model.Views.Should().Contain(v => v.QualifiedName.Name == "Active Users");
    }

    [Fact]
    public async Task WhenReadingViewThenDefinitionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        var view = model.Views.FirstOrDefault(v => v.QualifiedName.Name == "Active Users");
        view.Should().NotBeNull();

        view.Definition.Should().Contain("SELECT");
    }

    [Fact]
    public async Task WhenReadingViewThenColumnsArePopulated()
    {
        var model = await GetDatabaseModelAsync();

        var view = model.Views.FirstOrDefault(v => v.QualifiedName.Name == "Active Users");
        view.Should().NotBeNull();

        view.Columns.Should().Contain(c => c.Name == "Id");
        view.Columns.Should().Contain(c => c.Name == "UserName");
        view.Columns.Should().Contain(c => c.Name == "EmailAddress");
    }
}
