using SchemaSaurus.Metadata;
using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

public class IndexTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingTaskTableThenIndexesExist()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.Indexes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingTaskTableThenAssignedIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_Task_AssignedId");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenStatusIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_Task_StatusId");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenPriorityIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_Task_PriorityId");
    }

    [Fact]
    public async Task WhenReadingAssignedIdIndexThenColumnIsCorrect()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        var index = taskTable.Indexes.First(i => i.Name == "IX_Task_AssignedId");
        index.Columns.Should().Contain(c => c.ColumnName == "AssignedId");
    }

    [Fact]
    public async Task WhenReadingUniqueIndexThenIsUniqueIsTrue()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var uniqueIndex = userTable.Indexes.First(i => i.Name == "UX_User_EmailAddress");
        uniqueIndex.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingUniqueEmailIndexThenColumnIsEmailAddress()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var uniqueIndex = userTable.Indexes.First(i => i.Name == "UX_User_EmailAddress");
        uniqueIndex.Columns.Should().Contain(c => c.ColumnName == "EmailAddress");
    }

    [Fact]
    public async Task WhenReadingRoleTableThenUniqueNameIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var roleTable = model.Tables.First(t => t.QualifiedName.Name == "Role");

        roleTable.Indexes.Should().Contain(i => i.Name == "UX_Role_Name");
        var index = roleTable.Indexes.First(i => i.Name == "UX_Role_Name");
        index.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingNonUniqueIndexThenIsUniqueIsFalse()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        var index = taskTable.Indexes.First(i => i.Name == "IX_Task_StatusId");
        index.IsUnique.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingIndexColumnThenSortDirectionIsAscending()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        var index = taskTable.Indexes.First(i => i.Name == "IX_Task_StatusId");
        var keyColumns = index.Columns.Where(c => !c.IsIncludedColumn).ToList();
        keyColumns.Should().AllSatisfy(c => c.SortDirection.Should().Be(SortDirection.Ascending));
    }
}
