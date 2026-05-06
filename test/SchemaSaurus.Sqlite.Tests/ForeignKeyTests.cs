using SchemaSaurus.Metadata;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class ForeignKeyTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingTaskTableThenForeignKeysExist()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.ForeignKeys.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingTaskTableThenStatusForeignKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.ForeignKeys.Should().Contain(fk => fk.Name == "fk_Task_Status_StatusId");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenPriorityForeignKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.ForeignKeys.Should().Contain(fk => fk.Name == "fk_Task_Priority_PriorityId");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenAssignedForeignKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        taskTable.ForeignKeys.Should().Contain(fk => fk.Name == "fk_Task_User_AssignedId");
    }

    [Fact]
    public async Task WhenReadingStatusForeignKeyThenPrincipalTableIsStatus()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        var fk = taskTable.ForeignKeys.First(fk => fk.Name == "fk_Task_Status_StatusId");
        fk.PrincipalTableName.Schema.Should().BeNull();
        fk.PrincipalTableName.Name.Should().Be("Status");
    }

    [Fact]
    public async Task WhenReadingStatusForeignKeyThenColumnMappingsAreCorrect()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        var fk = taskTable.ForeignKeys.First(fk => fk.Name == "fk_Task_Status_StatusId");
        fk.ColumnMappings.Should().HaveCount(1);
        fk.ColumnMappings[0].DependentColumnName.Should().Be("StatusId");
        fk.ColumnMappings[0].PrincipalColumnName.Should().Be("Id");
    }

    [Fact]
    public async Task WhenReadingForeignKeyThenDefaultReferentialActionIsNoAction()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        var fk = taskTable.ForeignKeys.First(fk => fk.Name == "fk_Task_Status_StatusId");
        fk.OnDelete.Should().Be(ReferentialAction.NoAction);
        fk.OnUpdate.Should().Be(ReferentialAction.NoAction);
    }

    [Fact]
    public async Task WhenReadingUserRoleTableThenTwoForeignKeysExist()
    {
        var model = await GetDatabaseModelAsync();
        var userRoleTable = model.Tables.First(t => t.QualifiedName.Name == "UserRole");

        userRoleTable.ForeignKeys.Should().HaveCount(2);
        userRoleTable.ForeignKeys.Should().Contain(fk => fk.Name == "fk_UserRole_Role_RoleId");
        userRoleTable.ForeignKeys.Should().Contain(fk => fk.Name == "fk_UserRole_User_UserId");
    }

    [Fact]
    public async Task WhenReadingTaskExtendedTableThenForeignKeyToTaskExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskExtendedTable = model.Tables.First(t => t.QualifiedName.Name == "TaskExtended");

        taskExtendedTable.ForeignKeys.Should().Contain(fk => fk.Name == "fk_TaskExtended_Task_TaskId");
        var fk = taskExtendedTable.ForeignKeys.First(fk => fk.Name == "fk_TaskExtended_Task_TaskId");
        fk.PrincipalTableName.Name.Should().Be("Task");
    }
}
