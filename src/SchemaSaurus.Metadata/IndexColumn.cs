namespace SchemaSaurus.Metadata;

/// <summary>
/// Extends <see cref="ColumnReference"/> with covering-index support for an
/// <see cref="Index"/> column entry.
/// </summary>
/// <remarks>
/// An included (non-key) column participates in index leaf pages for covering queries
/// but does not contribute to the sort order of the index or its uniqueness evaluation.
/// Key columns inherit <see cref="ColumnReference.SortDirection"/> to express their
/// ordering within the index.
/// </remarks>
[Equatable]
public sealed partial class IndexColumn : ColumnReference
{
    /// <summary>
    /// Indicates whether this column is an included (non-key) column in a covering index
    /// (SQL Server <c>INCLUDE</c> clause, PostgreSQL <c>INCLUDE</c>).
    /// Included columns do not affect sort order and are ignored for uniqueness evaluation.
    /// Defaults to <see langword="false"/> for key columns.
    /// </summary>
    [JsonPropertyName("isIncludedColumn")]
    public bool IsIncludedColumn { get; init; }
}
