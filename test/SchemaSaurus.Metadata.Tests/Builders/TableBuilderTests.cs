using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class TableBuilderTests
{
    [Fact]
    public void WhenSchemaQualifiedNameSetThenBuildSucceeds()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .Build();

        table.QualifiedName.Schema.Should().Be("dbo");
        table.QualifiedName.Name.Should().Be("Orders");
        table.Columns.Should().BeEmpty();
        table.Indexes.Should().BeEmpty();
        table.Triggers.Should().BeEmpty();
        table.PrimaryKey.Should().BeNull();
        table.UniqueConstraints.Should().BeEmpty();
        table.CheckConstraints.Should().BeEmpty();
        table.ForeignKeys.Should().BeEmpty();
    }

    [Fact]
    public void WhenSchemaQualifiedNameStructSetThenBuildSucceeds()
    {
        var name = new SchemaQualifiedName { Schema = "sales", Name = "Invoices" };

        var table = new TableBuilder()
            .WithQualifiedName(name)
            .Build();

        table.QualifiedName.Should().Be(name);
    }

    [Fact]
    public void WhenColumnAddedViaBuilderActionThenColumnAppearsInTable()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Products")
            .AddColumn(c => c
                .WithName("Id")
                .WithOrdinalPosition(1)
                .WithIsNullable(false)
                .WithDbType(DbType.Int32)
                .WithNativeTypeName("int")
                .WithSystemType(typeof(int)))
            .Build();

        table.Columns.Should().ContainSingle()
            .Which.Name.Should().Be("Id");
    }

    [Fact]
    public void WhenColumnAddedDirectlyThenColumnAppearsInTable()
    {
        var column = new Column
        {
            Name = "Name",
            OrdinalPosition = 2,
            IsNullable = false,
            DbType = DbType.String,
            NativeTypeName = "nvarchar(100)",
            SystemType = typeof(string),
        };

        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Products")
            .AddColumn(column)
            .Build();

        table.Columns.Should().ContainSingle()
            .Which.Name.Should().Be("Name");
    }

    [Fact]
    public void WhenIndexAddedViaBuilderActionThenIndexAppearsInTable()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .AddIndex(ix => ix
                .WithName("IX_Orders_CustomerId")
                .AddColumn("CustomerId"))
            .Build();

        table.Indexes.Should().ContainSingle()
            .Which.Name.Should().Be("IX_Orders_CustomerId");
    }

    [Fact]
    public void WhenPrimaryKeySetViaConvenienceOverloadThenPrimaryKeyIsPopulated()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .WithPrimaryKey("PK_Orders", true,
                new ColumnReference { ColumnName = "Id" })
            .Build();

        table.PrimaryKey.Should().NotBeNull();
        table.PrimaryKey!.Name.Should().Be("PK_Orders");
        table.PrimaryKey.IsClustered.Should().BeTrue();
        table.PrimaryKey.Columns.Should().ContainSingle()
            .Which.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenUniqueConstraintAddedViaConvenienceOverloadThenConstraintAppearsInTable()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Users")
            .AddUniqueConstraint("UQ_Users_Email",
                new ColumnReference { ColumnName = "Email" })
            .Build();

        table.UniqueConstraints.Should().ContainSingle()
            .Which.Name.Should().Be("UQ_Users_Email");
    }

    [Fact]
    public void WhenCheckConstraintAddedViaConvenienceOverloadThenConstraintAppearsInTable()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Products")
            .AddCheckConstraint("CK_Products_Price", "([Price] > 0)")
            .Build();

        var ck = table.CheckConstraints.Should().ContainSingle().Subject;
        ck.Name.Should().Be("CK_Products_Price");
        ck.Expression.Should().Be("([Price] > 0)");
    }

    [Fact]
    public void WhenForeignKeyAddedViaBuilderActionThenForeignKeyAppearsInTable()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .AddForeignKey(fk => fk
                .WithName("FK_Order_Customer")
                .WithPrincipalTableName("dbo", "Customers")
                .AddColumnMapping("CustomerId", "Id")
                .WithOnDelete(ReferentialAction.Cascade))
            .Build();

        var foreignKey = table.ForeignKeys.Should().ContainSingle().Subject;
        foreignKey.Name.Should().Be("FK_Order_Customer");
        foreignKey.OnDelete.Should().Be(ReferentialAction.Cascade);
    }

    [Fact]
    public void WhenTriggerAddedThenTriggerAppearsInTable()
    {
        var trigger = new Trigger
        {
            Name = "TR_Orders_Audit",
            Timing = TriggerTiming.After,
            Events = TriggerEvent.Insert | TriggerEvent.Update,
        };

        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .AddTrigger(trigger)
            .Build();

        table.Triggers.Should().ContainSingle()
            .Which.Name.Should().Be("TR_Orders_Audit");
    }

    [Fact]
    public void WhenDescriptionSetThenTableHasDescription()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .WithDescription("Customer order header")
            .Build();

        table.Description.Should().Be("Customer order header");
    }

    [Fact]
    public void WhenOptionsSetThenTableHasOptions()
    {
        var options = new TableOptions { IsTemporalTable = true };

        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .WithOptions(options)
            .Build();

        table.Options.IsTemporalTable.Should().BeTrue();
    }

    [Fact]
    public void WhenAnnotationAddedThenTableHasAnnotation()
    {
        var table = new TableBuilder()
            .WithQualifiedName("dbo", "Orders")
            .WithAnnotation("engine", "InnoDB")
            .Build();

        table.Annotations.Should().ContainKey("engine").WhoseValue.Should().Be("InnoDB");
    }

    [Fact]
    public void WhenSchemaQualifiedNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new TableBuilder()
            .WithDescription("No name set");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithQualifiedName*");
    }
}
