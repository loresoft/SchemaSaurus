using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class ViewBuilderTests
{
    [Fact]
    public void WhenSchemaQualifiedNameSetThenBuildSucceeds()
    {
        var view = new ViewBuilder()
            .WithQualifiedName("dbo", "vw_ActiveCustomers")
            .Build();

        view.QualifiedName.Schema.Should().Be("dbo");
        view.QualifiedName.Name.Should().Be("vw_ActiveCustomers");
        view.IsMaterialized.Should().BeFalse();
        view.Definition.Should().BeNull();
        view.Columns.Should().BeEmpty();
        view.Indexes.Should().BeEmpty();
        view.Triggers.Should().BeEmpty();
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedView()
    {
        var view = new ViewBuilder()
            .WithQualifiedName(new SchemaQualifiedName { Schema = "public", Name = "mv_Summary" })
            .WithDefinition("SELECT * FROM Orders")
            .WithIsMaterialized(true)
            .WithDescription("Materialized summary view")
            .WithAnnotation("refresh_interval", "1h")
            .Build();

        view.Definition.Should().Be("SELECT * FROM Orders");
        view.IsMaterialized.Should().BeTrue();
        view.Description.Should().Be("Materialized summary view");
        view.Annotations.Should().ContainKey("refresh_interval");
    }

    [Fact]
    public void WhenColumnAddedViaBuilderActionThenColumnAppearsInView()
    {
        var view = new ViewBuilder()
            .WithQualifiedName("dbo", "vw_Test")
            .AddColumn(c => c
                .WithName("Id")
                .WithOrdinalPosition(1)
                .WithIsNullable(false)
                .WithDbType(DbType.Int32)
                .WithNativeTypeName("int")
                .WithSystemType(typeof(int)))
            .Build();

        view.Columns.Should().ContainSingle()
            .Which.Name.Should().Be("Id");
    }

    [Fact]
    public void WhenIndexAddedViaBuilderActionThenIndexAppearsInView()
    {
        var view = new ViewBuilder()
            .WithQualifiedName("dbo", "vw_Indexed")
            .AddIndex(ix => ix
                .WithName("IX_vw_Indexed_Col1")
                .WithIsClustered(true)
                .WithIsUnique(true)
                .AddColumn("Col1"))
            .Build();

        view.Indexes.Should().ContainSingle()
            .Which.Name.Should().Be("IX_vw_Indexed_Col1");
    }

    [Fact]
    public void WhenTriggerAddedThenTriggerAppearsInView()
    {
        var trigger = new Trigger
        {
            Name = "TR_View_InsteadOf",
            Timing = TriggerTiming.InsteadOf,
            Events = TriggerEvent.Insert,
        };

        var view = new ViewBuilder()
            .WithQualifiedName("dbo", "vw_Test")
            .AddTrigger(trigger)
            .Build();

        view.Triggers.Should().ContainSingle()
            .Which.Name.Should().Be("TR_View_InsteadOf");
    }

    [Fact]
    public void WhenSchemaQualifiedNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ViewBuilder()
            .WithDefinition("SELECT 1");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithQualifiedName*");
    }
}
