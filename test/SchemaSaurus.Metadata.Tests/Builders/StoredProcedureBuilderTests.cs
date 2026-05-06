using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class StoredProcedureBuilderTests
{
    [Fact]
    public void WhenSchemaQualifiedNameSetThenBuildSucceeds()
    {
        var sp = new StoredProcedureBuilder()
            .WithQualifiedName("dbo", "uspGetCustomer")
            .Build();

        sp.QualifiedName.Schema.Should().Be("dbo");
        sp.QualifiedName.Name.Should().Be("uspGetCustomer");
        sp.Parameters.Should().BeEmpty();
        sp.Definition.Should().BeNull();
        sp.Description.Should().BeNull();
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedStoredProcedure()
    {
        var sp = new StoredProcedureBuilder()
            .WithQualifiedName(new SchemaQualifiedName { Schema = "dbo", Name = "uspCreateOrder" })
            .WithDefinition("CREATE PROCEDURE dbo.uspCreateOrder AS BEGIN ... END")
            .WithDescription("Creates a new order")
            .WithAnnotation("owner", "admin")
            .Build();

        sp.Definition.Should().Contain("uspCreateOrder");
        sp.Description.Should().Be("Creates a new order");
        sp.Annotations.Should().ContainKey("owner");
    }

    [Fact]
    public void WhenParameterAddedViaBuilderActionThenParameterAppearsInProcedure()
    {
        var sp = new StoredProcedureBuilder()
            .WithQualifiedName("dbo", "uspGetCustomer")
            .AddParameter(p => p
                .WithName("@customerId")
                .WithOrdinal(1)
                .WithDbType(DbType.Int32)
                .WithNativeTypeName("int")
                .WithSystemType(typeof(int)))
            .Build();

        sp.Parameters.Should().ContainSingle()
            .Which.Name.Should().Be("@customerId");
    }

    [Fact]
    public void WhenParameterAddedDirectlyThenParameterAppearsInProcedure()
    {
        var param = new Parameter
        {
            Name = "@id",
            Ordinal = 1,
            DbType = DbType.Int32,
            NativeTypeName = "int",
            SystemType = typeof(int),
        };

        var sp = new StoredProcedureBuilder()
            .WithQualifiedName("dbo", "uspDelete")
            .AddParameter(param)
            .Build();

        sp.Parameters.Should().ContainSingle()
            .Which.Name.Should().Be("@id");
    }

    [Fact]
    public void WhenSchemaQualifiedNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new StoredProcedureBuilder()
            .WithDefinition("SELECT 1");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithQualifiedName*");
    }
}
