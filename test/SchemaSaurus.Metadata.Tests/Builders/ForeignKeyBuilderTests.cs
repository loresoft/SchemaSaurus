using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class ForeignKeyBuilderTests
{
    [Fact]
    public void WhenRequiredPropertiesSetThenBuildSucceeds()
    {
        var fk = new ForeignKeyBuilder()
            .WithName("FK_Order_Customer")
            .WithPrincipalTableName("dbo", "Customers")
            .Build();

        fk.Name.Should().Be("FK_Order_Customer");
        fk.PrincipalTableName.Schema.Should().Be("dbo");
        fk.PrincipalTableName.Name.Should().Be("Customers");
        fk.OnDelete.Should().Be(ReferentialAction.NoAction);
        fk.OnUpdate.Should().Be(ReferentialAction.NoAction);
        fk.IsDisabled.Should().BeFalse();
        fk.ColumnMappings.Should().BeEmpty();
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedForeignKey()
    {
        var fk = new ForeignKeyBuilder()
            .WithName("FK_Order_Customer")
            .WithPrincipalTableName(new SchemaQualifiedName { Schema = "dbo", Name = "Customers" })
            .AddColumnMapping("CustomerId", "Id")
            .AddColumnMapping("TenantId", "TenantId")
            .WithOnDelete(ReferentialAction.Cascade)
            .WithOnUpdate(ReferentialAction.SetNull)
            .WithIsDisabled(true)
            .WithAnnotation("provider:enforcedFK", true)
            .Build();

        fk.ColumnMappings.Should().HaveCount(2);
        fk.ColumnMappings[0].DependentColumnName.Should().Be("CustomerId");
        fk.ColumnMappings[0].PrincipalColumnName.Should().Be("Id");
        fk.ColumnMappings[1].DependentColumnName.Should().Be("TenantId");
        fk.OnDelete.Should().Be(ReferentialAction.Cascade);
        fk.OnUpdate.Should().Be(ReferentialAction.SetNull);
        fk.IsDisabled.Should().BeTrue();
        fk.Annotations.Should().ContainKey("provider:enforcedFK");
    }

    [Fact]
    public void WhenNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ForeignKeyBuilder()
            .WithPrincipalTableName("dbo", "Customers");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithName*");
    }

    [Fact]
    public void WhenPrincipalTableNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ForeignKeyBuilder()
            .WithName("FK_Order_Customer");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithPrincipalTableName*");
    }
}
