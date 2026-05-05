using System.Data;

using SchemaSaurus.Metadata.Extensions;

namespace SchemaSaurus.Metadata.Tests.Extensions;

public class NormalizeExtensionsTests
{
    [Fact]
    public void WhenDecimalPrecisionNormalizedThenPrecisionReturned()
    {
        int? precision = 18;

        var result = precision.NormalizePrecision(DbType.Decimal);

        result.Should().Be(18);
    }

    [Fact]
    public void WhenNonDecimalPrecisionNormalizedThenNullReturned()
    {
        int? precision = 18;

        var result = precision.NormalizePrecision(DbType.Int32);

        result.Should().BeNull();
    }

    [Fact]
    public void WhenNullDecimalPrecisionNormalizedThenNullReturned()
    {
        int? precision = null;

        var result = precision.NormalizePrecision(DbType.Decimal);

        result.Should().BeNull();
    }

    [Fact]
    public void WhenDecimalScaleNormalizedThenScaleReturned()
    {
        int? scale = 2;

        var result = scale.NormalizeScale(DbType.Decimal);

        result.Should().Be(2);
    }

    [Fact]
    public void WhenNonDecimalScaleNormalizedThenNullReturned()
    {
        int? scale = 2;

        var result = scale.NormalizeScale(DbType.String);

        result.Should().BeNull();
    }

    [Fact]
    public void WhenNullDecimalScaleNormalizedThenNullReturned()
    {
        int? scale = null;

        var result = scale.NormalizeScale(DbType.Decimal);

        result.Should().BeNull();
    }
}
