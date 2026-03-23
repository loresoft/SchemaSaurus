using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class ScalarFunctionBuilderTests
{
    [Fact]
    public void WhenRequiredPropertiesSetThenBuildSucceeds()
    {
        var fn = new ScalarFunctionBuilder()
            .WithSchemaQualifiedName("dbo", "fnGetTotal")
            .WithReturnType(DbType.Decimal, "decimal(18,2)", typeof(decimal))
            .Build();

        fn.SchemaQualifiedName.Name.Should().Be("fnGetTotal");
        fn.ReturnType.DbType.Should().Be(DbType.Decimal);
        fn.ReturnType.NativeTypeName.Should().Be("decimal(18,2)");
        fn.ReturnType.SystemType.Should().Be(typeof(decimal));
        fn.IsDeterministic.Should().BeFalse();
        fn.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedFunction()
    {
        var returnType = new TypeMapping
        {
            DbType = DbType.Int32,
            NativeTypeName = "int",
            SystemType = typeof(int),
        };

        var fn = new ScalarFunctionBuilder()
            .WithSchemaQualifiedName(new SchemaQualifiedName { Schema = "dbo", Name = "fnCalc" })
            .WithReturnType(returnType)
            .WithIsDeterministic(true)
            .WithDefinition("CREATE FUNCTION dbo.fnCalc() RETURNS int AS BEGIN RETURN 1 END")
            .WithAnnotation("schema_bound", true)
            .Build();

        fn.IsDeterministic.Should().BeTrue();
        fn.Definition.Should().Contain("fnCalc");
        fn.Annotations.Should().ContainKey("schema_bound");
    }

    [Fact]
    public void WhenParameterAddedViaBuilderActionThenParameterAppearsInFunction()
    {
        var fn = new ScalarFunctionBuilder()
            .WithSchemaQualifiedName("dbo", "fnAdd")
            .WithReturnType(DbType.Int32, "int", typeof(int))
            .AddParameter(p => p
                .WithName("@a")
                .WithOrdinal(1)
                .WithDbType(DbType.Int32)
                .WithNativeTypeName("int")
                .WithSystemType(typeof(int)))
            .AddParameter(p => p
                .WithName("@b")
                .WithOrdinal(2)
                .WithDbType(DbType.Int32)
                .WithNativeTypeName("int")
                .WithSystemType(typeof(int)))
            .Build();

        fn.Parameters.Should().HaveCount(2);
        fn.Parameters[0].Name.Should().Be("@a");
        fn.Parameters[1].Name.Should().Be("@b");
    }

    [Fact]
    public void WhenSchemaQualifiedNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ScalarFunctionBuilder()
            .WithReturnType(DbType.Int32, "int", typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithSchemaQualifiedName*");
    }

    [Fact]
    public void WhenReturnTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ScalarFunctionBuilder()
            .WithSchemaQualifiedName("dbo", "fnNoReturn");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithReturnType*");
    }
}
