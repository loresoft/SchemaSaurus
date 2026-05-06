using SchemaSaurus.Metadata;
using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

public class IndexTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingTaskTableThenIndexesExist()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "TASK");

        taskTable.Indexes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingTaskTableThenAssignedIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "TASK");

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_TASK_ASSIGNEDID");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenStatusIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "TASK");

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_TASK_STATUSID");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenPriorityIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "TASK");

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_TASK_PRIORITYID");
    }

    [Fact]
    public async Task WhenReadingAssignedIdIndexThenColumnIsCorrect()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "TASK");

        var index = taskTable.Indexes.First(i => i.Name == "IX_TASK_ASSIGNEDID");
        index.Columns.Should().Contain(c => c.ColumnName == "ASSIGNEDID");
    }

    [Fact]
    public async Task WhenReadingUniqueIndexThenIsUniqueIsTrue()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var uniqueIndex = userTable.Indexes.First(i => i.Name == "UX_USER_EMAILADDRESS");
        uniqueIndex.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingUniqueEmailIndexThenColumnIsEmailAddress()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var uniqueIndex = userTable.Indexes.First(i => i.Name == "UX_USER_EMAILADDRESS");
        uniqueIndex.Columns.Should().Contain(c => c.ColumnName == "EMAILADDRESS");
    }

    [Fact]
    public async Task WhenReadingRoleTableThenUniqueNameIndexExists()
    {
        var model = await GetDatabaseModelAsync();
        var roleTable = model.Tables.First(t => t.QualifiedName.Name == "ROLE");

        roleTable.Indexes.Should().Contain(i => i.Name == "UX_ROLE_NAME");
        var index = roleTable.Indexes.First(i => i.Name == "UX_ROLE_NAME");
        index.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingNonUniqueIndexThenIsUniqueIsFalse()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "TASK");

        var index = taskTable.Indexes.First(i => i.Name == "IX_TASK_STATUSID");
        index.IsUnique.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingIndexColumnThenSortDirectionIsAscending()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "TASK");

        var index = taskTable.Indexes.First(i => i.Name == "IX_TASK_STATUSID");
        var keyColumns = index.Columns.Where(c => !c.IsIncludedColumn).ToList();
        keyColumns.Should().AllSatisfy(c => c.SortDirection.Should().Be(SortDirection.Ascending));
    }
}
