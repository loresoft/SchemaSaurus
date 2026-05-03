namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="Index"/> instance,
/// typically populated from a <see cref="System.Data.Common.DbDataReader"/> row.
/// </summary>
public sealed class IndexBuilder
{
    private string? _name;
    private bool _isUnique;
    private bool _isClustered;
    private bool _isFiltered;
    private string? _filterExpression;
    private string? _indexType;
    private int? _fillFactor;
    private bool _isDisabled;
    private readonly List<IndexColumn> _columns = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the index name.</summary>
    public IndexBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets whether the index enforces uniqueness.</summary>
    public IndexBuilder WithIsUnique(bool isUnique)
    {
        _isUnique = isUnique;
        return this;
    }

    /// <summary>Sets whether the index is physically clustered.</summary>
    public IndexBuilder WithIsClustered(bool isClustered)
    {
        _isClustered = isClustered;
        return this;
    }

    /// <summary>Sets whether this is a filtered or partial index.</summary>
    public IndexBuilder WithIsFiltered(bool isFiltered)
    {
        _isFiltered = isFiltered;
        return this;
    }

    /// <summary>Sets the SQL predicate for a filtered or partial index.</summary>
    public IndexBuilder WithFilterExpression(string? filterExpression)
    {
        _filterExpression = filterExpression;
        return this;
    }

    /// <summary>Sets the index access method (e.g., <c>"BTREE"</c>, <c>"HASH"</c>).</summary>
    public IndexBuilder WithIndexType(string? indexType)
    {
        _indexType = indexType;
        return this;
    }

    /// <summary>Sets the fill factor percentage.</summary>
    public IndexBuilder WithFillFactor(int? fillFactor)
    {
        _fillFactor = fillFactor;
        return this;
    }

    /// <summary>Sets whether the index is currently disabled.</summary>
    public IndexBuilder WithIsDisabled(bool isDisabled)
    {
        _isDisabled = isDisabled;
        return this;
    }

    /// <summary>Adds a key column to the index.</summary>
    public IndexBuilder AddColumn(string columnName, SortDirection sortDirection = SortDirection.Ascending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        _columns.Add(new IndexColumn
        {
            ColumnName = columnName,
            SortDirection = sortDirection,
            IsIncludedColumn = false,
        });
        return this;
    }

    /// <summary>Adds an included (non-key) column to the index.</summary>
    public IndexBuilder AddIncludedColumn(string columnName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        _columns.Add(new IndexColumn
        {
            ColumnName = columnName,
            IsIncludedColumn = true,
        });
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public IndexBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="Index"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="Index"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the index name has not been set.
    /// </exception>
    public Index Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException(
                $"An index name is required. Call {nameof(WithName)} before {nameof(Build)}.");
        }

        return new Index
        {
            Name = _name!,
            IsUnique = _isUnique,
            IsClustered = _isClustered,
            IsFiltered = _isFiltered,
            FilterExpression = _filterExpression,
            IndexType = _indexType,
            FillFactor = _fillFactor,
            IsDisabled = _isDisabled,
            Columns = _columns,
            Annotations = _annotations,
        };
    }
}
