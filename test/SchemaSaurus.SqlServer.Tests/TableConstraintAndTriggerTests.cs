using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

public class TableConstraintAndTriggerTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingStatusTableThenCheckConstraintIsDiscovered()
    {
        var model = await GetStatusTableModelAsync();
        var statusTable = model.Tables.Single(t => t.QualifiedName.Name == "Status");

        statusTable.CheckConstraints.Should().ContainSingle(c => c.Name == "CK_Status_DisplayOrder_NonNegative")
            .Which.Expression.Should().Contain("[DisplayOrder]");
    }

    [Fact]
    public async Task WhenReadingStatusTableThenTriggerIsDiscovered()
    {
        var model = await GetStatusTableModelAsync();
        var statusTable = model.Tables.Single(t => t.QualifiedName.Name == "Status");

        statusTable.Triggers.Should().ContainSingle(t => t.Name == "TR_Status_Audit");
    }

    [Fact]
    public async Task WhenReadingStatusTableTriggerThenTriggerMetadataIsPopulated()
    {
        var model = await GetStatusTableModelAsync();
        var statusTable = model.Tables.Single(t => t.QualifiedName.Name == "Status");
        var trigger = statusTable.Triggers.Single(t => t.Name == "TR_Status_Audit");

        trigger.Timing.Should().Be(TriggerTiming.After);
        trigger.Events.Should().Be(TriggerEvent.Insert | TriggerEvent.Update);
        trigger.IsDisabled.Should().BeFalse();
        trigger.Definition.Should().Contain("CREATE");
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
