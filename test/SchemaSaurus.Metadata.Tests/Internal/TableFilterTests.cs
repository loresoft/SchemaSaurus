using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Internal;

namespace SchemaSaurus.Metadata.Tests.Internal;

public class TableFilterTests
{
    [Fact]
    public void WhenTablesQualifiedThenSchemaPinned()
    {
        var filter = TableFilter.Build(["sales.orders", "sales.customers"], "ns.nspname", "cls.relname", BuildInClause);

        filter.Should().Be("(ns.nspname IN ('sales') AND cls.relname IN ('orders', 'customers'))");
    }

    [Fact]
    public void WhenTablesMixedThenConditionsCombinedWithOr()
    {
        var filter = TableFilter.Build(["orders", "shipping.orders"], "ns.nspname", "cls.relname", BuildInClause);

        filter.Should().Be("(cls.relname IN ('orders') OR (ns.nspname IN ('shipping') AND cls.relname IN ('orders')))");
    }

    [Fact]
    public void WhenSchemasDifferOnlyByCaseThenEachKeepsItsOwnSpelling()
    {
        var filter = TableFilter.Build(["Sales.orders", "sales.customers"], "ns.nspname", "cls.relname", BuildInClause);

        filter.Should().Be(
            "((ns.nspname IN ('Sales') AND cls.relname IN ('orders')) "
            + "OR (ns.nspname IN ('sales') AND cls.relname IN ('customers')))");
    }

    [Fact]
    public void WhenEntryHasMultipleDotsThenFirstDotSeparatesSchema()
    {
        var filter = TableFilter.Build(["catalog.sales.orders"], "ns.nspname", "cls.relname", BuildInClause);

        filter.Should().Be("(ns.nspname IN ('catalog') AND cls.relname IN ('sales.orders'))");
    }

    [Fact]
    public void WhenEntryQualifiedThenOnlyMatchingSchemaMatched()
    {
        TableFilter.IsMatch([], "main", "orders").Should().BeTrue();
        TableFilter.IsMatch(["orders"], "main", "ORDERS").Should().BeTrue();
        TableFilter.IsMatch(["main.orders"], "main", "orders").Should().BeTrue();
        TableFilter.IsMatch(["sales.orders"], "main", "orders").Should().BeFalse();
    }

    private static string BuildInClause(IReadOnlyCollection<string> values, string expression)
        => $"{expression} IN ({string.Join(", ", values.Select(value => value.EscapeLiteral()))})";
}
