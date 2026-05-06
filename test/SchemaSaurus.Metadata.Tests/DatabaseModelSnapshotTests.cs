using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests;

public class DatabaseModelSnapshotTests
{
    [Fact]
    public Task WhenFullModelSerializedThenJsonMatchesSnapshot()
    {
        var model = DatabaseModelFixtures.CreateFullModel();
        var json = model.ToJson();

        return Verify(json, "json");
    }

    [Fact]
    public Task WhenMinimalModelSerializedThenJsonMatchesSnapshot()
    {
        var model = DatabaseModelFixtures.CreateMinimalModel();
        var json = model.ToJson();

        return Verify(json, "json");
    }

    [Fact]
    public Task WhenTableWithAllConstraintTypesSerializedThenJsonMatchesSnapshot()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("ConstraintDb")
            .WithProvider("SqlServer")
            .WithDefaultSchemaName("dbo")
            .AddTable(t => t
                .WithQualifiedName("dbo", "Products")
                .WithDescription("Product catalog")
                .WithPrimaryKey("PK_Products", true,
                    new ColumnReference { ColumnName = "Id" })
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))
                    .WithIsIdentity(true)
                    .WithIdentitySeed(1)
                    .WithIdentityIncrement(1))
                .AddColumn(c => c
                    .WithName("Sku")
                    .WithOrdinalPosition(2)
                    .WithIsNullable(false)
                    .WithDbType(DbType.String)
                    .WithNativeTypeName("varchar(50)")
                    .WithSystemType(typeof(string))
                    .WithMaxLength(50)
                    .WithIsUnicode(false)
                    .WithIsFixedLength(false))
                .AddColumn(c => c
                    .WithName("Price")
                    .WithOrdinalPosition(3)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Decimal)
                    .WithNativeTypeName("decimal(18,2)")
                    .WithSystemType(typeof(decimal))
                    .WithPrecision(18)
                    .WithScale(2))
                .AddColumn(c => c
                    .WithName("ComputedTax")
                    .WithOrdinalPosition(4)
                    .WithIsNullable(true)
                    .WithDbType(DbType.Decimal)
                    .WithNativeTypeName("decimal(18,2)")
                    .WithSystemType(typeof(decimal?))
                    .WithIsComputed(true)
                    .WithComputedColumnSql("([Price] * 0.08)")
                    .WithIsStored(true))
                .AddUniqueConstraint("UQ_Products_Sku",
                    new ColumnReference { ColumnName = "Sku" })
                .AddCheckConstraint("CK_Products_Price", "([Price] > 0)")
                .AddIndex(ix => ix
                    .WithName("IX_Products_Sku")
                    .WithIsUnique(true)
                    .WithIsFiltered(true)
                    .WithFilterExpression("([Price] > 0)")
                    .WithIndexType("BTREE")
                    .WithFillFactor(90)
                    .AddColumn("Sku")
                    .AddIncludedColumn("Price")))
            .Build();

        var json = model.ToJson();

        return Verify(json, "json");
    }
}
