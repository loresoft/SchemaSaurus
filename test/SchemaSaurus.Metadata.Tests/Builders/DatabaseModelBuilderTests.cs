using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class DatabaseModelBuilderTests
{
    [Fact]
    public void WhenRequiredPropertiesSetThenBuildSucceeds()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .Build();

        model.DatabaseName.Should().Be("TestDb");
        model.Provider.Should().Be("SqlServer");
        model.Tables.Should().BeEmpty();
        model.Views.Should().BeEmpty();
        model.Sequences.Should().BeEmpty();
        model.StoredProcedures.Should().BeEmpty();
        model.ScalarFunctions.Should().BeEmpty();
        model.TableValuedFunctions.Should().BeEmpty();
        model.UserDefinedTypes.Should().BeEmpty();
    }

    [Fact]
    public void WhenAllMetadataPropertiesSetThenBuildReturnsFullModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("PostgreSql")
            .WithCollation("en_US.utf8")
            .WithDefaultSchemaName("public")
            .WithServerVersion("16.2")
            .WithAnnotation("encoding", "UTF8")
            .Build();

        model.Collation.Should().Be("en_US.utf8");
        model.DefaultSchemaName.Should().Be("public");
        model.ServerVersion.Should().Be("16.2");
        model.Annotations.Should().ContainKey("encoding").WhoseValue.Should().Be("UTF8");
    }

    [Fact]
    public void WhenTableAddedViaBuilderActionThenTableAppearsInModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddTable(t => t
                .WithSchemaQualifiedName("dbo", "Orders")
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))))
            .Build();

        model.Tables.Should().ContainSingle()
            .Which.SchemaQualifiedName.Name.Should().Be("Orders");
    }

    [Fact]
    public void WhenViewAddedViaBuilderActionThenViewAppearsInModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddView(v => v
                .WithSchemaQualifiedName("dbo", "vw_ActiveOrders")
                .WithDefinition("SELECT * FROM Orders WHERE IsActive = 1"))
            .Build();

        model.Views.Should().ContainSingle()
            .Which.SchemaQualifiedName.Name.Should().Be("vw_ActiveOrders");
    }

    [Fact]
    public void WhenSequenceAddedViaBuilderActionThenSequenceAppearsInModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddSequence(s => s
                .WithSchemaQualifiedName("dbo", "OrderSeq")
                .WithSystemType(typeof(long))
                .WithStartValue(1)
                .WithIncrement(1)
                .WithMinValue(1)
                .WithMaxValue(long.MaxValue))
            .Build();

        model.Sequences.Should().ContainSingle()
            .Which.SchemaQualifiedName.Name.Should().Be("OrderSeq");
    }

    [Fact]
    public void WhenStoredProcedureAddedViaBuilderActionThenProcedureAppearsInModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddStoredProcedure(sp => sp
                .WithSchemaQualifiedName("dbo", "uspGetOrders"))
            .Build();

        model.StoredProcedures.Should().ContainSingle()
            .Which.SchemaQualifiedName.Name.Should().Be("uspGetOrders");
    }

    [Fact]
    public void WhenScalarFunctionAddedViaBuilderActionThenFunctionAppearsInModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddScalarFunction(fn => fn
                .WithSchemaQualifiedName("dbo", "fnGetTotal")
                .WithReturnType(DbType.Decimal, "decimal(18,2)", typeof(decimal)))
            .Build();

        model.ScalarFunctions.Should().ContainSingle()
            .Which.SchemaQualifiedName.Name.Should().Be("fnGetTotal");
    }

    [Fact]
    public void WhenTableValuedFunctionAddedViaBuilderActionThenFunctionAppearsInModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddTableValuedFunction(fn => fn
                .WithSchemaQualifiedName("dbo", "fnGetItems"))
            .Build();

        model.TableValuedFunctions.Should().ContainSingle()
            .Which.SchemaQualifiedName.Name.Should().Be("fnGetItems");
    }

    [Fact]
    public void WhenUserDefinedTypeAddedViaBuilderActionThenTypeAppearsInModel()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddUserDefinedType(udt => udt
                .WithSchemaQualifiedName("dbo", "PhoneNumber")
                .WithKind(UserDefinedTypeKind.Alias)
                .WithDbType(DbType.String)
                .WithNativeTypeName("nvarchar(20)")
                .WithSystemType(typeof(string)))
            .Build();

        model.UserDefinedTypes.Should().ContainSingle()
            .Which.SchemaQualifiedName.Name.Should().Be("PhoneNumber");
    }

    [Fact]
    public void WhenDatabaseNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new DatabaseModelBuilder()
            .WithProvider("SqlServer");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithDatabaseName*");
    }

    [Fact]
    public void WhenProviderMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithProvider*");
    }
}
