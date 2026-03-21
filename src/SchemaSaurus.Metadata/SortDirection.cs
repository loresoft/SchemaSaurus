namespace SchemaSaurus.Metadata;

/// <summary>
/// Sort direction for a column in an index, primary key, or unique constraint.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SortDirection>))]
public enum SortDirection
{
    /// <summary>Ascending sort order (default).</summary>
    Ascending,

    /// <summary>Descending sort order.</summary>
    Descending,
}
