using System.Data;

using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

public class ViewSchemaTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingViewsThenViewsAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        model.Views.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingViewsThenPriorityDropdownViewExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Views.Should().Contain(v => v.QualifiedName.Name == "PriorityDropdown");
    }

    [Fact]
    public async Task WhenReadingViewsThenStatusDropdownViewExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Views.Should().Contain(v => v.QualifiedName.Name == "StatusDropdown");
    }

    [Fact]
    public async Task WhenReadingPriorityDropdownViewThenSchemaIsDefaultSchema()
    {
        var model = await GetDatabaseModelAsync();
        var view = model.Views.First(v => v.QualifiedName.Name == "PriorityDropdown");

        view.QualifiedName.Schema.Should().Be(model.DefaultSchemaName);
    }

    [Fact]
    public async Task WhenReadingPriorityDropdownViewThenColumnsExist()
    {
        var model = await GetDatabaseModelAsync();
        var view = model.Views.First(v => v.QualifiedName.Name == "PriorityDropdown");

        view.Columns.Should().HaveCount(3);
        view.Columns.Should().Contain(c => c.Name == "Id");
        view.Columns.Should().Contain(c => c.Name == "Name");
        view.Columns.Should().Contain(c => c.Name == "DisplayOrder");
    }

    [Fact]
    public async Task WhenReadingPriorityDropdownViewThenIdColumnIsInt32()
    {
        var model = await GetDatabaseModelAsync();
        var view = model.Views.First(v => v.QualifiedName.Name == "PriorityDropdown");

        var idColumn = view.Columns.First(c => c.Name == "Id");
        idColumn.DbType.Should().Be(DbType.Int32);
        idColumn.SystemType.Should().Be(typeof(int));
    }

    [Fact]
    public async Task WhenReadingViewWithDefinitionsThenDefinitionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var view = model.Views.First(v => v.QualifiedName.Name == "PriorityDropdown");

        view.Definition.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenExcludingViewsThenNoViewsReturned()
    {
        var options = new Metadata.Provider.SchemaReaderOptions
        {
            IncludeViews = false
        };

        var model = await GetDatabaseModelAsync(options);

        model.Views.Should().BeEmpty();
    }
}
