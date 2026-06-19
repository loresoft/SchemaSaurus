using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class TableTriggerTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingStatusTableThenTriggersAreDiscovered()
    {
        var model = await GetStatusTableModelAsync();
        var statusTable = model.Tables.Single(t => t.QualifiedName.Name == "Status");

        statusTable.Triggers.Should().Contain(t => t.Name == "TR_Status_Audit");
        statusTable.Triggers.Should().Contain(t => t.Name == "TR_Status_PreventDelete");
    }

    [Fact]
    public async Task WhenReadingAfterStatusTableTriggerThenTriggerMetadataIsPopulated()
    {
        var model = await GetStatusTableModelAsync();
        var statusTable = model.Tables.Single(t => t.QualifiedName.Name == "Status");
        var trigger = statusTable.Triggers.Single(t => t.Name == "TR_Status_Audit");

        trigger.Timing.Should().Be(TriggerTiming.After);
        trigger.Events.Should().Be(TriggerEvent.Insert | TriggerEvent.Update);
        trigger.IsDisabled.Should().BeFalse();
        trigger.Definition.Should().Contain("CREATE TRIGGER");
    }

    [Fact]
    public async Task WhenReadingDisabledBeforeStatusTableTriggerThenTriggerMetadataIsPopulated()
    {
        var model = await GetStatusTableModelAsync();
        var statusTable = model.Tables.Single(t => t.QualifiedName.Name == "Status");
        var trigger = statusTable.Triggers.Single(t => t.Name == "TR_Status_PreventDelete");

        trigger.Timing.Should().Be(TriggerTiming.Before);
        trigger.Events.Should().Be(TriggerEvent.Delete);
        trigger.IsDisabled.Should().BeTrue();
        trigger.Definition.Should().Contain("CREATE TRIGGER");
    }

    private Task<DatabaseModel> GetStatusTableModelAsync()
    {
        var options = new SchemaReaderOptions
        {
            Tables = ["Status"],
            IncludeViews = false,
            IncludeStoredProcedures = false,
            IncludeScalarFunctions = false,
            IncludeTableValuedFunctions = false,
            IncludeSequences = false,
            IncludeUserDefinedTypes = false,
        };

        return GetDatabaseModelAsync(options);
    }
}
