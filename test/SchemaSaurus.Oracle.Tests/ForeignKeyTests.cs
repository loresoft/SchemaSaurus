using SchemaSaurus.Metadata;
using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

public class ForeignKeyTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingTaskTableThenForeignKeysExist()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASK");

        taskTable.ForeignKeys.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingTaskTableThenStatusForeignKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASK");

        taskTable.ForeignKeys.Should().Contain(fk => fk.Name == "FK_TASK_STATUS_STATUSID");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenPriorityForeignKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASK");

        taskTable.ForeignKeys.Should().Contain(fk => fk.Name == "FK_TASK_PRIORITY_PRIORITYID");
    }

    [Fact]
    public async Task WhenReadingTaskTableThenAssignedForeignKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASK");

        taskTable.ForeignKeys.Should().Contain(fk => fk.Name == "FK_TASK_USER_ASSIGNEDID");
    }

    [Fact]
    public async Task WhenReadingStatusForeignKeyThenPrincipalTableIsStatus()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASK");

        var fk = taskTable.ForeignKeys.First(fk => fk.Name == "FK_TASK_STATUS_STATUSID");
        fk.PrincipalTableName.Schema.Should().Be(model.DefaultSchemaName);
        fk.PrincipalTableName.Name.Should().Be("STATUS");
    }

    [Fact]
    public async Task WhenReadingStatusForeignKeyThenColumnMappingsAreCorrect()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASK");

        var fk = taskTable.ForeignKeys.First(fk => fk.Name == "FK_TASK_STATUS_STATUSID");
        fk.ColumnMappings.Should().HaveCount(1);
        fk.ColumnMappings[0].DependentColumnName.Should().Be("STATUSID");
        fk.ColumnMappings[0].PrincipalColumnName.Should().Be("ID");
    }

    [Fact]
    public async Task WhenReadingForeignKeyThenDefaultReferentialActionIsNoAction()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASK");

        var fk = taskTable.ForeignKeys.First(fk => fk.Name == "FK_TASK_STATUS_STATUSID");
        fk.OnDelete.Should().Be(ReferentialAction.NoAction);
        fk.OnUpdate.Should().Be(ReferentialAction.NoAction);
    }

    [Fact]
    public async Task WhenReadingUserRoleTableThenTwoForeignKeysExist()
    {
        var model = await GetDatabaseModelAsync();
        var userRoleTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "USERROLE");

        userRoleTable.ForeignKeys.Should().HaveCount(2);
        userRoleTable.ForeignKeys.Should().Contain(fk => fk.Name == "FK_USERROLE_USER_USERID");
        userRoleTable.ForeignKeys.Should().Contain(fk => fk.Name == "FK_USERROLE_ROLE_ROLEID");
    }

    [Fact]
    public async Task WhenReadingTaskExtendedTableThenForeignKeyToTaskExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskExtendedTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "TASKEXTENDED");

        taskExtendedTable.ForeignKeys.Should().Contain(fk => fk.Name == "FK_TASKEXTENDED_TASK_TASKID");
        var fk = taskExtendedTable.ForeignKeys.First(fk => fk.Name == "FK_TASKEXTENDED_TASK_TASKID");
        fk.PrincipalTableName.Name.Should().Be("TASK");
    }
}
