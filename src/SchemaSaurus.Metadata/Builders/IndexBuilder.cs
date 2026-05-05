namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="Index"/> instance,
/// typically populated from a <see cref="System.Data.Common.DbDataReader"/> row.
/// </summary>
public sealed class IndexBuilder : IAnnotationBuilder<IndexBuilder>
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
    /// <param name="name">The index name.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets whether the index enforces uniqueness.</summary>
    /// <param name="isUnique"><see langword="true"/> to enforce uniqueness; otherwise, <see langword="false"/>.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithIsUnique(bool isUnique)
    {
        _isUnique = isUnique;
        return this;
    }

    /// <summary>Sets whether the index is physically clustered.</summary>
    /// <param name="isClustered"><see langword="true"/> if the index is clustered; otherwise, <see langword="false"/>.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithIsClustered(bool isClustered)
    {
        _isClustered = isClustered;
        return this;
    }

    /// <summary>Sets whether this is a filtered or partial index.</summary>
    /// <param name="isFiltered"><see langword="true"/> if the index is filtered/partial; otherwise, <see langword="false"/>.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithIsFiltered(bool isFiltered)
    {
        _isFiltered = isFiltered;
        return this;
    }

    /// <summary>Sets the SQL predicate for a filtered or partial index.</summary>
    /// <param name="filterExpression">The filter predicate, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithFilterExpression(string? filterExpression)
    {
        _filterExpression = filterExpression;
        return this;
    }

    /// <summary>Sets the index access method (e.g., <c>"BTREE"</c>, <c>"HASH"</c>).</summary>
    /// <param name="indexType">The index type name, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithIndexType(string? indexType)
    {
        _indexType = indexType;
        return this;
    }

    /// <summary>Sets the fill factor percentage.</summary>
    /// <param name="fillFactor">The fill factor percentage, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithFillFactor(int? fillFactor)
    {
        _fillFactor = fillFactor;
        return this;
    }

    /// <summary>Sets whether the index is currently disabled.</summary>
    /// <param name="isDisabled"><see langword="true"/> to mark the index as disabled; otherwise, <see langword="false"/>.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    public IndexBuilder WithIsDisabled(bool isDisabled)
    {
        _isDisabled = isDisabled;
        return this;
    }

    /// <summary>Adds a key column to the index.</summary>
    /// <param name="columnName">The column name.</param>
    /// <param name="sortDirection">The column sort direction. Defaults to <see cref="SortDirection.Ascending"/>.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="columnName"/> is <see langword="null"/>, empty, or whitespace.</exception>
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
    /// <param name="columnName">The included column name.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="columnName"/> is <see langword="null"/>, empty, or whitespace.</exception>
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
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current <see cref="IndexBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public IndexBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="Index"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="Index"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithName"/> has not been called.
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
