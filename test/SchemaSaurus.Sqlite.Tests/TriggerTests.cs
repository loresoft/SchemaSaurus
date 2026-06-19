using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class TriggerTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingTriggerTargetTableThenTriggersAreDiscovered()
    {
        var model = await GetTriggerTargetTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "TriggerTarget");

        table.Triggers.Should().Contain(t => t.Name == "TR_TriggerTarget_AfterInsert");
        table.Triggers.Should().Contain(t => t.Name == "TR_TriggerTarget_BeforeUpdate");
        table.Triggers.Should().Contain(t => t.Name == "TR_TriggerTarget_AfterDelete");
    }

    [Fact]
    public async Task WhenReadingAfterInsertTriggerThenMetadataIsPopulated()
    {
        var model = await GetTriggerTargetTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "TriggerTarget");
        var trigger = table.Triggers.Single(t => t.Name == "TR_TriggerTarget_AfterInsert");

        trigger.Timing.Should().Be(TriggerTiming.After);
        trigger.Events.Should().Be(TriggerEvent.Insert);
        trigger.IsDisabled.Should().BeFalse();
        trigger.Definition.Should().Contain("CREATE TRIGGER");
    }

    [Fact]
    public async Task WhenReadingBeforeUpdateTriggerThenMetadataIsPopulated()
    {
        var model = await GetTriggerTargetTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "TriggerTarget");
        var trigger = table.Triggers.Single(t => t.Name == "TR_TriggerTarget_BeforeUpdate");

        trigger.Timing.Should().Be(TriggerTiming.Before);
        trigger.Events.Should().Be(TriggerEvent.Update);
        trigger.IsDisabled.Should().BeFalse();
        trigger.Definition.Should().Contain("CREATE TRIGGER");
    }

    [Fact]
    public async Task WhenReadingAfterDeleteTriggerThenMetadataIsPopulated()
    {
        var model = await GetTriggerTargetTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "TriggerTarget");
        var trigger = table.Triggers.Single(t => t.Name == "TR_TriggerTarget_AfterDelete");

        trigger.Timing.Should().Be(TriggerTiming.After);
        trigger.Events.Should().Be(TriggerEvent.Delete);
        trigger.IsDisabled.Should().BeFalse();
        trigger.Definition.Should().Contain("CREATE TRIGGER");
    }

    private Task<DatabaseModel> GetTriggerTargetTableModelAsync()
    {
        var options = new SchemaReaderOptions
        {
            Tables = ["TriggerTarget"],
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
