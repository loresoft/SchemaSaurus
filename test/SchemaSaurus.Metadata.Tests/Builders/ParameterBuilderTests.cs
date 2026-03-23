using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class ParameterBuilderTests
{
    [Fact]
    public void WhenRequiredPropertiesSetThenBuildSucceeds()
    {
        var param = new ParameterBuilder()
            .WithName("@customerId")
            .WithOrdinal(1)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int))
            .Build();

        param.Name.Should().Be("@customerId");
        param.Ordinal.Should().Be(1);
        param.Direction.Should().Be(ParameterDirection.Input);
        param.DbType.Should().Be(DbType.Int32);
        param.NativeTypeName.Should().Be("int");
        param.SystemType.Should().Be(typeof(int));
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedParameter()
    {
        var param = new ParameterBuilder()
            .WithName("@amount")
            .WithOrdinal(2)
            .WithDirection(ParameterDirection.Output)
            .WithDefaultValueSql("0")
            .WithDbType(DbType.Decimal)
            .WithNativeTypeName("decimal(18,2)")
            .WithSystemType(typeof(decimal))
            .WithMaxLength(null)
            .WithPrecision(18)
            .WithScale(2)
            .WithIsUnicode(null)
            .WithIsFixedLength(null)
            .WithAnnotation("hint", "output-param")
            .Build();

        param.Direction.Should().Be(ParameterDirection.Output);
        param.DefaultValueSql.Should().Be("0");
        param.Precision.Should().Be(18);
        param.Scale.Should().Be(2);
        param.Annotations.Should().ContainKey("hint").WhoseValue.Should().Be("output-param");
    }

    [Fact]
    public void WhenNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ParameterBuilder()
            .WithOrdinal(1)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithName*");
    }

    [Fact]
    public void WhenOrdinalMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ParameterBuilder()
            .WithName("@id")
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithOrdinal*");
    }

    [Fact]
    public void WhenDbTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ParameterBuilder()
            .WithName("@id")
            .WithOrdinal(1)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithDbType*");
    }

    [Fact]
    public void WhenNativeTypeNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ParameterBuilder()
            .WithName("@id")
            .WithOrdinal(1)
            .WithDbType(DbType.Int32)
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithNativeTypeName*");
    }

    [Fact]
    public void WhenSystemTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new ParameterBuilder()
            .WithName("@id")
            .WithOrdinal(1)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithSystemType*");
    }
}
