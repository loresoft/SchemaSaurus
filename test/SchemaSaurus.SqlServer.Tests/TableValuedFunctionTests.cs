using System.Data;

using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

public class TableValuedFunctionTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingTableValuedFunctionsThenGetStatusDropdownExists()
    {
        var model = await GetDatabaseModelAsync();

        model.TableValuedFunctions.Should().Contain(f => f.QualifiedName.Name == "GetStatusDropdown");
    }

    [Fact]
    public async Task WhenReadingGetStatusDropdownThenReturnColumnsAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();
        var function = model.TableValuedFunctions.First(f => f.QualifiedName.Name == "GetStatusDropdown");

        function.ReturnColumns.Should().HaveCount(3);
        function.ReturnColumns.Select(c => c.Name).Should().Equal("Id", "Name", "DisplayOrder");
    }

    [Fact]
    public async Task WhenReadingGetStatusDropdownThenReturnColumnMetadataIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var function = model.TableValuedFunctions.First(f => f.QualifiedName.Name == "GetStatusDropdown");

        var idColumn = function.ReturnColumns.First(c => c.Name == "Id");
        idColumn.OrdinalPosition.Should().Be(1);
        idColumn.DbType.Should().Be(DbType.Int32);
        idColumn.SystemType.Should().Be(typeof(int));
        idColumn.IsNullable.Should().BeFalse();

        var nameColumn = function.ReturnColumns.First(c => c.Name == "Name");
        nameColumn.OrdinalPosition.Should().Be(2);
        nameColumn.DbType.Should().Be(DbType.String);
        nameColumn.SystemType.Should().Be(typeof(string));
        nameColumn.IsNullable.Should().BeFalse();
    }
}
