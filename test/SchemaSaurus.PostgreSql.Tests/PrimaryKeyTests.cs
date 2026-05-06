using SchemaSaurus.Metadata;
using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class PrimaryKeyTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingUserTableThenPrimaryKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        userTable.PrimaryKey.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenReadingUserTableThenPrimaryKeyHasIdColumn()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        userTable.PrimaryKey!.Columns.Should().HaveCount(1);
        userTable.PrimaryKey.Columns[0].ColumnName.Should().Be("Id");
    }

    [Fact]
    public async Task WhenReadingUserTableThenPrimaryKeyIsNotClustered()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        userTable.PrimaryKey!.IsClustered.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingUserRoleTableThenPrimaryKeyHasCompositeColumns()
    {
        var model = await GetDatabaseModelAsync();
        var userRoleTable = model.Tables.First(t => t.QualifiedName.Name == "UserRole");

        userRoleTable.PrimaryKey.Should().NotBeNull();
        userRoleTable.PrimaryKey!.Columns.Should().HaveCount(2);
        userRoleTable.PrimaryKey.Columns.Should().Contain(c => c.ColumnName == "UserId");
        userRoleTable.PrimaryKey.Columns.Should().Contain(c => c.ColumnName == "RoleId");
    }

    [Fact]
    public async Task WhenReadingPrimaryKeyThenSortDirectionIsAscending()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        userTable.PrimaryKey!.Columns[0].SortDirection.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public async Task WhenReadingPrimaryKeyThenNameIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        userTable.PrimaryKey!.Name.Should().NotBeNullOrWhiteSpace();
    }
}
