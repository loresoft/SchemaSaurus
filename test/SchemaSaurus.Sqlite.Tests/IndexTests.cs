using SchemaSaurus.Metadata;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class IndexTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingTaskTableThenIndexesExist()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        taskTable.Indexes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingTaskTableThenAssignedIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_Task_AssignedId");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenStatusIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_Task_StatusId");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenPriorityIdIndexExists()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        taskTable.Indexes.Should().Contain(i => i.Name == "IX_Task_PriorityId");
    }

    [Fact]
    public async Task WhenReadingAssignedIdIndexThenColumnIsCorrect()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        var index = taskTable.Indexes.FirstOrDefault(i => i.Name == "IX_Task_AssignedId");
        index.Should().NotBeNull();
        index.Columns.Should().Contain(c => c.ColumnName == "AssignedId");
    }

    [Fact]
    public async Task WhenReadingUniqueIndexThenIsUniqueIsTrue()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var uniqueIndex = userTable.Indexes.FirstOrDefault(i => i.Name == "UX_User_EmailAddress");
        uniqueIndex.Should().NotBeNull();
        uniqueIndex.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingUniqueEmailIndexThenColumnIsEmailAddress()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var uniqueIndex = userTable.Indexes.FirstOrDefault(i => i.Name == "UX_User_EmailAddress");
        uniqueIndex.Should().NotBeNull();
        uniqueIndex.Columns.Should().Contain(c => c.ColumnName == "EmailAddress");
    }

    [Fact]
    public async Task WhenReadingRoleTableThenUniqueNameIndexExists()
    {
        var model = await GetDatabaseModelAsync();

        var roleTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Role");
        roleTable.Should().NotBeNull();

        roleTable.Indexes.Should().Contain(i => i.Name == "UX_Role_Name");

        var index = roleTable.Indexes.FirstOrDefault(i => i.Name == "UX_Role_Name");
        index.Should().NotBeNull();
        index.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingNonUniqueIndexThenIsUniqueIsFalse()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        var index = taskTable.Indexes.FirstOrDefault(i => i.Name == "IX_Task_StatusId");
        index.Should().NotBeNull();
        index.IsUnique.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingIndexColumnThenSortDirectionIsAscending()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        var index = taskTable.Indexes.FirstOrDefault(i => i.Name == "IX_Task_StatusId");
        index.Should().NotBeNull();

        var keyColumns = index.Columns.Where(c => !c.IsIncludedColumn).ToList();
        keyColumns.Should().AllSatisfy(c => c.SortDirection.Should().Be(SortDirection.Ascending));
    }

    [Fact]
    public async Task WhenReadingPartialIndexThenFilterExpressionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        var computedTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Computed Column");
        computedTable.Should().NotBeNull();

        var index = computedTable.Indexes.FirstOrDefault(i => i.Name == "IX_Computed Column_Search Name_Active");
        index.Should().NotBeNull();
        index.IsFiltered.Should().BeTrue();
        index.FilterExpression.Should().Be("\"Last Name\" <> ''");
    }

    [Fact]
    public async Task WhenReadingExpressionIndexThenIndexDoesNotThrowAndIsDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        var computedTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Computed Column");
        computedTable.Should().NotBeNull();

        computedTable.Indexes.Should().Contain(i => i.Name == "IX_Computed Column_Lower_First Name");
    }
}
