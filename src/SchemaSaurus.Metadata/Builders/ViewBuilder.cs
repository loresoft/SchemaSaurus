namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="View"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class ViewBuilder : IAnnotationBuilder<ViewBuilder>
{
    private SchemaQualifiedName? _schemaQualifiedName;
    private string? _description;
    private string? _definition;
    private bool _isMaterialized;
    private readonly List<Column> _columns = [];
    private readonly List<Index> _indexes = [];
    private readonly List<Trigger> _triggers = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the view.</summary>
    public ViewBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and view name strings.</summary>
    public ViewBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the view description or comment.</summary>
    public ViewBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the SQL definition of the view.</summary>
    public ViewBuilder WithDefinition(string? definition)
    {
        _definition = definition;
        return this;
    }

    /// <summary>Sets whether this is a materialized or indexed view.</summary>
    public ViewBuilder WithIsMaterialized(bool isMaterialized)
    {
        _isMaterialized = isMaterialized;
        return this;
    }

    /// <summary>Adds a column to the view.</summary>
    public ViewBuilder AddColumn(Column column)
    {
        ArgumentNullException.ThrowIfNull(column);
        _columns.Add(column);
        return this;
    }

    /// <summary>Adds a column to the view using a builder action.</summary>
    public ViewBuilder AddColumn(Action<ColumnBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ColumnBuilder();
        configure(builder);
        _columns.Add(builder.Build());
        return this;
    }

    /// <summary>Adds an index to the view.</summary>
    public ViewBuilder AddIndex(Index index)
    {
        ArgumentNullException.ThrowIfNull(index);
        _indexes.Add(index);
        return this;
    }

    /// <summary>Adds an index to the view using a builder action.</summary>
    public ViewBuilder AddIndex(Action<IndexBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new IndexBuilder();
        configure(builder);
        _indexes.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a trigger to the view.</summary>
    public ViewBuilder AddTrigger(Trigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        _triggers.Add(trigger);
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public ViewBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="View"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="View"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the schema-qualified name has not been set.
    /// </exception>
    public View Build()
    {
        if (_schemaQualifiedName is null)
        {
            throw new InvalidOperationException(
                $"A schema-qualified name is required. Call {nameof(WithSchemaQualifiedName)} before {nameof(Build)}.");
        }

        return new View
        {
            SchemaQualifiedName = _schemaQualifiedName.Value,
            Description = _description,
            Definition = _definition,
            IsMaterialized = _isMaterialized,
            Columns = _columns,
            Indexes = _indexes,
            Triggers = _triggers,
            Annotations = _annotations,
        };
    }
}
