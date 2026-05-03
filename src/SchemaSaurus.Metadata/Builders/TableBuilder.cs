namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="Table"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows
/// across multiple catalog queries.
/// </summary>
public sealed class TableBuilder : IAnnotationBuilder<TableBuilder>
{
    private SchemaQualifiedName? _schemaQualifiedName;
    private string? _description;
    private PrimaryKey? _primaryKey;
    private TableOptions _options = new();
    private readonly List<Column> _columns = [];
    private readonly List<Index> _indexes = [];
    private readonly List<Trigger> _triggers = [];
    private readonly List<UniqueConstraint> _uniqueConstraints = [];
    private readonly List<CheckConstraint> _checkConstraints = [];
    private readonly List<ForeignKey> _foreignKeys = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the table.</summary>
    public TableBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and table name strings.</summary>
    public TableBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the table description or comment.</summary>
    public TableBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Adds a column to the table.</summary>
    public TableBuilder AddColumn(Column column)
    {
        ArgumentNullException.ThrowIfNull(column);
        _columns.Add(column);
        return this;
    }

    /// <summary>Adds a column to the table using a builder action.</summary>
    public TableBuilder AddColumn(Action<ColumnBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ColumnBuilder();
        configure(builder);
        _columns.Add(builder.Build());
        return this;
    }

    /// <summary>Adds an index to the table.</summary>
    public TableBuilder AddIndex(Index index)
    {
        ArgumentNullException.ThrowIfNull(index);
        _indexes.Add(index);
        return this;
    }

    /// <summary>Adds an index to the table using a builder action.</summary>
    public TableBuilder AddIndex(Action<IndexBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new IndexBuilder();
        configure(builder);
        _indexes.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a trigger to the table.</summary>
    public TableBuilder AddTrigger(Trigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        _triggers.Add(trigger);
        return this;
    }

    /// <summary>Sets the primary key constraint.</summary>
    public TableBuilder WithPrimaryKey(PrimaryKey? primaryKey)
    {
        _primaryKey = primaryKey;
        return this;
    }

    /// <summary>Sets the primary key using a name and column references.</summary>
    public TableBuilder WithPrimaryKey(string name, bool isClustered, params ColumnReference[] columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _primaryKey = new PrimaryKey
        {
            Name = name,
            IsClustered = isClustered,
            Columns = columns,
        };
        return this;
    }

    /// <summary>Adds a unique constraint to the table.</summary>
    public TableBuilder AddUniqueConstraint(UniqueConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        _uniqueConstraints.Add(constraint);
        return this;
    }

    /// <summary>Adds a unique constraint using a name and column references.</summary>
    public TableBuilder AddUniqueConstraint(string name, params ColumnReference[] columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _uniqueConstraints.Add(new UniqueConstraint
        {
            Name = name,
            Columns = columns,
        });
        return this;
    }

    /// <summary>Adds a check constraint to the table.</summary>
    public TableBuilder AddCheckConstraint(CheckConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        _checkConstraints.Add(constraint);
        return this;
    }

    /// <summary>Adds a check constraint using a name and expression.</summary>
    public TableBuilder AddCheckConstraint(string name, string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        _checkConstraints.Add(new CheckConstraint
        {
            Name = name,
            Expression = expression,
        });
        return this;
    }

    /// <summary>Adds a foreign key constraint to the table.</summary>
    public TableBuilder AddForeignKey(ForeignKey foreignKey)
    {
        ArgumentNullException.ThrowIfNull(foreignKey);
        _foreignKeys.Add(foreignKey);
        return this;
    }

    /// <summary>Adds a foreign key constraint using a builder action.</summary>
    public TableBuilder AddForeignKey(Action<ForeignKeyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ForeignKeyBuilder();
        configure(builder);
        _foreignKeys.Add(builder.Build());
        return this;
    }

    /// <summary>Sets the provider-specific table options.</summary>
    public TableBuilder WithOptions(TableOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public TableBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="Table"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="Table"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the schema-qualified name has not been set.
    /// </exception>
    public Table Build()
    {
        if (_schemaQualifiedName is null)
        {
            throw new InvalidOperationException(
                $"A schema-qualified name is required. Call {nameof(WithSchemaQualifiedName)} before {nameof(Build)}.");
        }

        return new Table
        {
            SchemaQualifiedName = _schemaQualifiedName.Value,
            Description = _description,
            Columns = _columns,
            Indexes = _indexes,
            Triggers = _triggers,
            PrimaryKey = _primaryKey,
            UniqueConstraints = _uniqueConstraints,
            CheckConstraints = _checkConstraints,
            ForeignKeys = _foreignKeys,
            Options = _options,
            Annotations = _annotations,
        };
    }
}
