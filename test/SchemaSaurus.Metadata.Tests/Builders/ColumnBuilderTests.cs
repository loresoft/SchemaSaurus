using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class ColumnBuilderTests
{
    [Fact]
    public void WhenAllRequiredPropertiesSetThenBuildSucceeds()
    {
        var column = new ColumnBuilder()
            .WithName("Id")
            .WithOrdinalPosition(1)
            .WithIsNullable(false)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int))
            .Build();

        column.Name.Should().Be("Id");
        column.OrdinalPosition.Should().Be(1);
        column.IsNullable.Should().BeFalse();
        column.DbType.Should().Be(DbType.Int32);
        column.NativeTypeName.Should().Be("int");
        column.SystemType.Should().Be(typeof(int));
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedColumn()
    {
        var column = new ColumnBuilder()
            .WithName("Price")
            .WithOrdinalPosition(3)
            .WithIsNullable(true)
            .WithDefaultValueSql("0.00")
            .WithIsIdentity(false)
            .WithIsComputed(false)
            .WithIsRowVersion(false)
            .WithIsConcurrencyToken(false)
            .WithCollation("SQL_Latin1_General_CP1_CI_AS")
            .WithDescription("Unit price")
            .WithDbType(DbType.Decimal)
            .WithNativeTypeName("decimal(18,2)")
            .WithSystemType(typeof(decimal?))
            .WithMaxLength(null)
            .WithPrecision(18)
            .WithScale(2)
            .WithIsUnicode(null)
            .WithIsFixedLength(null)
            .WithAnnotation("custom", "value")
            .Build();

        column.Name.Should().Be("Price");
        column.OrdinalPosition.Should().Be(3);
        column.IsNullable.Should().BeTrue();
        column.DefaultValueSql.Should().Be("0.00");
        column.Collation.Should().Be("SQL_Latin1_General_CP1_CI_AS");
        column.Description.Should().Be("Unit price");
        column.Precision.Should().Be(18);
        column.Scale.Should().Be(2);
        column.Annotations.Should().ContainKey("custom").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void WhenIdentityPropertiesSetThenBuildReturnsIdentityColumn()
    {
        var column = new ColumnBuilder()
            .WithName("Id")
            .WithOrdinalPosition(1)
            .WithIsNullable(false)
            .WithDbType(DbType.Int64)
            .WithNativeTypeName("bigint")
            .WithSystemType(typeof(long))
            .WithIsIdentity(true)
            .WithIdentitySeed(1)
            .WithIdentityIncrement(1)
            .Build();

        column.IsIdentity.Should().BeTrue();
        column.IdentitySeed.Should().Be(1);
        column.IdentityIncrement.Should().Be(1);
    }

    [Fact]
    public void WhenComputedPropertiesSetThenBuildReturnsComputedColumn()
    {
        var column = new ColumnBuilder()
            .WithName("Total")
            .WithOrdinalPosition(5)
            .WithIsNullable(true)
            .WithDbType(DbType.Decimal)
            .WithNativeTypeName("decimal(18,2)")
            .WithSystemType(typeof(decimal?))
            .WithIsComputed(true)
            .WithComputedColumnSql("([Quantity] * [UnitPrice])")
            .WithIsStored(true)
            .Build();

        column.IsComputed.Should().BeTrue();
        column.ComputedColumnSql.Should().Be("([Quantity] * [UnitPrice])");
        column.IsStored.Should().BeTrue();
    }

    [Fact]
    public void WhenRowVersionSetThenBuildReturnsConcurrencyColumn()
    {
        var column = new ColumnBuilder()
            .WithName("RowVersion")
            .WithOrdinalPosition(10)
            .WithIsNullable(false)
            .WithDbType(DbType.Binary)
            .WithNativeTypeName("rowversion")
            .WithSystemType(typeof(byte[]))
            .WithIsRowVersion(true)
            .WithIsConcurrencyToken(true)
            .Build();

        column.IsRowVersion.Should().BeTrue();
        column.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void WhenNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ColumnBuilder()
            .WithOrdinalPosition(1)
            .WithIsNullable(false)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithName*");
    }

    [Fact]
    public void WhenOrdinalPositionMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ColumnBuilder()
            .WithName("Id")
            .WithIsNullable(false)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithOrdinalPosition*");
    }

    [Fact]
    public void WhenIsNullableMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ColumnBuilder()
            .WithName("Id")
            .WithOrdinalPosition(1)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithIsNullable*");
    }

    [Fact]
    public void WhenDbTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ColumnBuilder()
            .WithName("Id")
            .WithOrdinalPosition(1)
            .WithIsNullable(false)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithDbType*");
    }

    [Fact]
    public void WhenNativeTypeNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ColumnBuilder()
            .WithName("Id")
            .WithOrdinalPosition(1)
            .WithIsNullable(false)
            .WithDbType(DbType.Int32)
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithNativeTypeName*");
    }

    [Fact]
    public void WhenSystemTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ColumnBuilder()
            .WithName("Id")
            .WithOrdinalPosition(1)
            .WithIsNullable(false)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithSystemType*");
    }

    [Fact]
    public void WhenStringColumnThenUnicodeAndFixedLengthAreSet()
    {
        var column = new ColumnBuilder()
            .WithName("Name")
            .WithOrdinalPosition(2)
            .WithIsNullable(false)
            .WithDbType(DbType.String)
            .WithNativeTypeName("nvarchar(256)")
            .WithSystemType(typeof(string))
            .WithMaxLength(256)
            .WithIsUnicode(true)
            .WithIsFixedLength(false)
            .Build();

        column.MaxLength.Should().Be(256);
        column.IsUnicode.Should().BeTrue();
        column.IsFixedLength.Should().BeFalse();
    }
}
