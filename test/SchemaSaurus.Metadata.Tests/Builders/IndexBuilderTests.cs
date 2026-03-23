using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class IndexBuilderTests
{
    [Fact]
    public void WhenNameSetThenBuildSucceeds()
    {
        var index = new IndexBuilder()
            .WithName("IX_Orders_CustomerId")
            .Build();

        index.Name.Should().Be("IX_Orders_CustomerId");
        index.IsUnique.Should().BeFalse();
        index.IsClustered.Should().BeFalse();
        index.Columns.Should().BeEmpty();
    }

    [Fact]
    public void WhenAllPropertiesSetThenBuildReturnsFullyPopulatedIndex()
    {
        var index = new IndexBuilder()
            .WithName("IX_Filtered")
            .WithIsUnique(true)
            .WithIsClustered(true)
            .WithIsFiltered(true)
            .WithFilterExpression("([IsActive] = 1)")
            .WithIndexType("BTREE")
            .WithFillFactor(80)
            .WithIsDisabled(true)
            .AddColumn("CustomerId", SortDirection.Ascending)
            .AddColumn("OrderDate", SortDirection.Descending)
            .AddIncludedColumn("TotalAmount")
            .WithAnnotation("custom", 42)
            .Build();

        index.Name.Should().Be("IX_Filtered");
        index.IsUnique.Should().BeTrue();
        index.IsClustered.Should().BeTrue();
        index.IsFiltered.Should().BeTrue();
        index.FilterExpression.Should().Be("([IsActive] = 1)");
        index.IndexType.Should().Be("BTREE");
        index.FillFactor.Should().Be(80);
        index.IsDisabled.Should().BeTrue();
        index.Columns.Should().HaveCount(3);
        index.Annotations.Should().ContainKey("custom").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void WhenKeyColumnAddedThenColumnIsNotIncluded()
    {
        var index = new IndexBuilder()
            .WithName("IX_Test")
            .AddColumn("Col1", SortDirection.Descending)
            .Build();

        var col = index.Columns.Should().ContainSingle().Subject;
        col.ColumnName.Should().Be("Col1");
        col.SortDirection.Should().Be(SortDirection.Descending);
        col.IsIncludedColumn.Should().BeFalse();
    }

    [Fact]
    public void WhenIncludedColumnAddedThenColumnIsMarkedAsIncluded()
    {
        var index = new IndexBuilder()
            .WithName("IX_Covering")
            .AddIncludedColumn("Payload")
            .Build();

        var col = index.Columns.Should().ContainSingle().Subject;
        col.ColumnName.Should().Be("Payload");
        col.IsIncludedColumn.Should().BeTrue();
    }

    [Fact]
    public void WhenNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new IndexBuilder()
            .WithIsUnique(true);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithName*");
    }

    [Fact]
    public void WhenDefaultSortDirectionUsedThenColumnIsAscending()
    {
        var index = new IndexBuilder()
            .WithName("IX_Default")
            .AddColumn("Col1")
            .Build();

        index.Columns[0].SortDirection.Should().Be(SortDirection.Ascending);
    }
}
