using SchemaSaurus.Metadata;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class PrimaryKeyTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingUserTableThenPrimaryKeyExists()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.SchemaQualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        userTable.PrimaryKey.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenReadingUserTableThenPrimaryKeyHasIdColumn()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.SchemaQualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var primaryKey = userTable.PrimaryKey;
        primaryKey.Should().NotBeNull();
        primaryKey.Columns.Should().HaveCount(1);
        primaryKey.Columns[0].ColumnName.Should().Be("Id");
    }

    [Fact]
    public async Task WhenReadingUserTableThenPrimaryKeyIsNotClustered()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.SchemaQualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var primaryKey = userTable.PrimaryKey;
        primaryKey.Should().NotBeNull();
        primaryKey.IsClustered.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingUserRoleTableThenPrimaryKeyHasCompositeColumns()
    {
        var model = await GetDatabaseModelAsync();

        var userRoleTable = model.Tables.FirstOrDefault(t => t.SchemaQualifiedName.Name == "UserRole");
        userRoleTable.Should().NotBeNull();

        userRoleTable.PrimaryKey.Should().NotBeNull();
        userRoleTable.PrimaryKey.Columns.Should().HaveCount(2);
        userRoleTable.PrimaryKey.Columns.Should().Contain(c => c.ColumnName == "UserId");
        userRoleTable.PrimaryKey.Columns.Should().Contain(c => c.ColumnName == "RoleId");
    }

    [Fact]
    public async Task WhenReadingPrimaryKeyThenSortDirectionIsAscending()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.SchemaQualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var primaryKey = userTable.PrimaryKey;
        primaryKey.Should().NotBeNull();
        primaryKey.Columns[0].SortDirection.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public async Task WhenReadingPrimaryKeyThenNameIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.SchemaQualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var primaryKey = userTable.PrimaryKey;
        primaryKey.Should().NotBeNull();
        primaryKey.Name.Should().Be("pk_User");
    }
}
