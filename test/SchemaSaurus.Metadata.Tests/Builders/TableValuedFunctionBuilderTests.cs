using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class TableValuedFunctionBuilderTests
{
    [Fact]
    public void WhenSchemaQualifiedNameSetThenBuildSucceeds()
    {
        var fn = new TableValuedFunctionBuilder()
            .WithSchemaQualifiedName("dbo", "fnGetOrders")
            .Build();

        fn.SchemaQualifiedName.Name.Should().Be("fnGetOrders");
        fn.Parameters.Should().BeEmpty();
        fn.ReturnColumns.Should().BeEmpty();
        fn.Definition.Should().BeNull();
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedFunction()
    {
        var fn = new TableValuedFunctionBuilder()
            .WithSchemaQualifiedName("dbo", "fnGetItems")
            .WithDefinition("CREATE FUNCTION dbo.fnGetItems() RETURNS TABLE AS RETURN SELECT 1")
            .AddReturnColumn("Id", 1, DbType.Int32, "int", typeof(int))
            .AddReturnColumn("Name", 2, DbType.String, "nvarchar(100)", typeof(string), isNullable: true)
            .WithAnnotation("inline", true)
            .Build();

        fn.Definition.Should().Contain("fnGetItems");
        fn.ReturnColumns.Should().HaveCount(2);
        fn.ReturnColumns[0].Name.Should().Be("Id");
        fn.ReturnColumns[0].IsNullable.Should().BeFalse();
        fn.ReturnColumns[1].Name.Should().Be("Name");
        fn.ReturnColumns[1].IsNullable.Should().BeTrue();
        fn.Annotations.Should().ContainKey("inline");
    }

    [Fact]
    public void WhenReturnColumnAddedDirectlyThenColumnAppearsInFunction()
    {
        var returnCol = new ReturnColumn
        {
            Name = "Value",
            OrdinalPosition = 1,
            DbType = DbType.Decimal,
            NativeTypeName = "decimal(18,2)",
            SystemType = typeof(decimal),
        };

        var fn = new TableValuedFunctionBuilder()
            .WithSchemaQualifiedName("dbo", "fnCalc")
            .AddReturnColumn(returnCol)
            .Build();

        fn.ReturnColumns.Should().ContainSingle()
            .Which.Name.Should().Be("Value");
    }

    [Fact]
    public void WhenParameterAddedViaBuilderActionThenParameterAppearsInFunction()
    {
        var fn = new TableValuedFunctionBuilder()
            .WithSchemaQualifiedName("dbo", "fnSearch")
            .AddParameter(p => p
                .WithName("@keyword")
                .WithOrdinal(1)
                .WithDbType(DbType.String)
                .WithNativeTypeName("nvarchar(256)")
                .WithSystemType(typeof(string)))
            .Build();

        fn.Parameters.Should().ContainSingle()
            .Which.Name.Should().Be("@keyword");
    }

    [Fact]
    public void WhenSchemaQualifiedNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new TableValuedFunctionBuilder()
            .WithDefinition("SELECT 1");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithSchemaQualifiedName*");
    }
}
