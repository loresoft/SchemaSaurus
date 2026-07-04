using SchemaSaurus.Metadata.Extensions;

namespace SchemaSaurus.Metadata.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", false)]
    [InlineData("value", false)]
    public void WhenStringCheckedForNullOrEmptyThenExpectedResultReturned(string? value, bool expected)
    {
        var result = value.IsNullOrEmpty();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("value", false)]
    public void WhenStringCheckedForNullOrWhiteSpaceThenExpectedResultReturned(string? value, bool expected)
    {
        var result = value.IsNullOrWhiteSpace();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", true)]
    [InlineData("value", true)]
    public void WhenStringCheckedForValueThenExpectedResultReturned(string? value, bool expected)
    {
        var result = value.HasValue();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" ", " ")]
    [InlineData("value", "value")]
    public void WhenNullIfEmptyCalledThenExpectedValueReturned(string? value, string? expected)
    {
        var result = value.NullIfEmpty();

        result.Should().Be(expected);
    }

    [Fact]
    public void WhenNullLiteralEscapedThenSqlNullLiteralReturned()
    {
        const string? value = null;

        var result = value.EscapeLiteral();

        result.Should().Be("NULL");
    }

    [Fact]
    public void WhenLiteralWithoutDelimiterEscapedThenValueIsDelimited()
    {
        var result = "value".EscapeLiteral();

        result.Should().Be("'value'");
    }

    [Fact]
    public void WhenLiteralWithDelimiterEscapedThenDelimiterIsEscaped()
    {
        var result = "Bob's".EscapeLiteral();

        result.Should().Be("'Bob''s'");
    }

    [Fact]
    public void WhenLiteralWithCustomDelimiterEscapedThenCustomDelimiterIsEscaped()
    {
        var result = "a`b".EscapeLiteral('`', '\\');

        result.Should().Be("`a`\\b`");
    }
}
