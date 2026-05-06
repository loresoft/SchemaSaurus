using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class SequenceBuilderTests
{
    [Fact]
    public void WhenAllRequiredPropertiesSetThenBuildSucceeds()
    {
        var sequence = new SequenceBuilder()
            .WithQualifiedName("dbo", "OrderSeq")
            .WithSystemType(typeof(long))
            .WithStartValue(1)
            .WithIncrement(1)
            .WithMinValue(1)
            .WithMaxValue(long.MaxValue)
            .Build();

        sequence.QualifiedName.Name.Should().Be("OrderSeq");
        sequence.DbType.Should().Be(DbType.Int64);
        sequence.SystemType.Should().Be(typeof(long));
        sequence.StartValue.Should().Be(1);
        sequence.Increment.Should().Be(1);
        sequence.MinValue.Should().Be(1);
        sequence.MaxValue.Should().Be(long.MaxValue);
        sequence.IsCycling.Should().BeFalse();
        sequence.CacheSize.Should().BeNull();
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedSequence()
    {
        var sequence = new SequenceBuilder()
            .WithQualifiedName(new SchemaQualifiedName { Schema = "public", Name = "id_seq" })
            .WithDbType(DbType.Int32)
            .WithSystemType(typeof(int))
            .WithStartValue(100)
            .WithIncrement(10)
            .WithMinValue(1)
            .WithMaxValue(1_000_000)
            .WithIsCycling(true)
            .WithCacheSize(50)
            .WithAnnotation("owned_by", "orders.id")
            .Build();

        sequence.DbType.Should().Be(DbType.Int32);
        sequence.StartValue.Should().Be(100);
        sequence.Increment.Should().Be(10);
        sequence.IsCycling.Should().BeTrue();
        sequence.CacheSize.Should().Be(50);
        sequence.Annotations.Should().ContainKey("owned_by");
    }

    [Fact]
    public void WhenSchemaQualifiedNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new SequenceBuilder()
            .WithSystemType(typeof(long))
            .WithStartValue(1)
            .WithIncrement(1)
            .WithMinValue(1)
            .WithMaxValue(long.MaxValue);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithQualifiedName*");
    }

    [Fact]
    public void WhenSystemTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new SequenceBuilder()
            .WithQualifiedName("dbo", "Seq1")
            .WithStartValue(1)
            .WithIncrement(1)
            .WithMinValue(1)
            .WithMaxValue(long.MaxValue);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithSystemType*");
    }

    [Fact]
    public void WhenStartValueMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new SequenceBuilder()
            .WithQualifiedName("dbo", "Seq1")
            .WithSystemType(typeof(long))
            .WithIncrement(1)
            .WithMinValue(1)
            .WithMaxValue(long.MaxValue);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithStartValue*");
    }

    [Fact]
    public void WhenIncrementMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new SequenceBuilder()
            .WithQualifiedName("dbo", "Seq1")
            .WithSystemType(typeof(long))
            .WithStartValue(1)
            .WithMinValue(1)
            .WithMaxValue(long.MaxValue);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithIncrement*");
    }

    [Fact]
    public void WhenMinValueMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new SequenceBuilder()
            .WithQualifiedName("dbo", "Seq1")
            .WithSystemType(typeof(long))
            .WithStartValue(1)
            .WithIncrement(1)
            .WithMaxValue(long.MaxValue);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithMinValue*");
    }

    [Fact]
    public void WhenMaxValueMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new SequenceBuilder()
            .WithQualifiedName("dbo", "Seq1")
            .WithSystemType(typeof(long))
            .WithStartValue(1)
            .WithIncrement(1)
            .WithMinValue(1);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithMaxValue*");
    }
}
