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
    /// <param name="name">The schema-qualified table name.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    public TableBuilder WithQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and table name strings.</summary>
    /// <param name="schema">The schema name, or <see langword="null"/> when no schema is provided.</param>
    /// <param name="name">The table name.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public TableBuilder WithQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the table description or comment.</summary>
    /// <param name="description">The table description, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    public TableBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Adds a column to the table.</summary>
    /// <param name="column">The column to add.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="column"/> is <see langword="null"/>.</exception>
    public TableBuilder AddColumn(Column column)
    {
        ArgumentNullException.ThrowIfNull(column);
        _columns.Add(column);
        return this;
    }

    /// <summary>Adds a column to the table using a builder action.</summary>
    /// <param name="configure">An action that configures a <see cref="ColumnBuilder"/> instance.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public TableBuilder AddColumn(Action<ColumnBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ColumnBuilder();
        configure(builder);
        _columns.Add(builder.Build());
        return this;
    }

    /// <summary>Adds an index to the table.</summary>
    /// <param name="index">The index to add.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="index"/> is <see langword="null"/>.</exception>
    public TableBuilder AddIndex(Index index)
    {
        ArgumentNullException.ThrowIfNull(index);
        _indexes.Add(index);
        return this;
    }

    /// <summary>Adds an index to the table using a builder action.</summary>
    /// <param name="configure">An action that configures an <see cref="IndexBuilder"/> instance.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public TableBuilder AddIndex(Action<IndexBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new IndexBuilder();
        configure(builder);
        _indexes.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a trigger to the table.</summary>
    /// <param name="trigger">The trigger to add.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="trigger"/> is <see langword="null"/>.</exception>
    public TableBuilder AddTrigger(Trigger trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        _triggers.Add(trigger);
        return this;
    }

    /// <summary>Sets the primary key constraint.</summary>
    /// <param name="primaryKey">The primary key definition, or <see langword="null"/> to clear it.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    public TableBuilder WithPrimaryKey(PrimaryKey? primaryKey)
    {
        _primaryKey = primaryKey;
        return this;
    }

    /// <summary>Sets the primary key using a name and column references.</summary>
    /// <param name="name">The primary key constraint name.</param>
    /// <param name="isClustered"><see langword="true"/> if clustered; otherwise, <see langword="false"/>.</param>
    /// <param name="columns">The primary key column references.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
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
    /// <param name="constraint">The unique constraint to add.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="constraint"/> is <see langword="null"/>.</exception>
    public TableBuilder AddUniqueConstraint(UniqueConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        _uniqueConstraints.Add(constraint);
        return this;
    }

    /// <summary>Adds a unique constraint using a name and column references.</summary>
    /// <param name="name">The unique constraint name.</param>
    /// <param name="columns">The unique constraint column references.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
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
    /// <param name="constraint">The check constraint to add.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="constraint"/> is <see langword="null"/>.</exception>
    public TableBuilder AddCheckConstraint(CheckConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        _checkConstraints.Add(constraint);
        return this;
    }

    /// <summary>Adds a check constraint using a name and expression.</summary>
    /// <param name="name">The check constraint name.</param>
    /// <param name="expression">The check expression.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="expression"/> is <see langword="null"/>, empty, or whitespace.</exception>
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
    /// <param name="foreignKey">The foreign key to add.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="foreignKey"/> is <see langword="null"/>.</exception>
    public TableBuilder AddForeignKey(ForeignKey foreignKey)
    {
        ArgumentNullException.ThrowIfNull(foreignKey);
        _foreignKeys.Add(foreignKey);
        return this;
    }

    /// <summary>Adds a foreign key constraint using a builder action.</summary>
    /// <param name="configure">An action that configures a <see cref="ForeignKeyBuilder"/> instance.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public TableBuilder AddForeignKey(Action<ForeignKeyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ForeignKeyBuilder();
        configure(builder);
        _foreignKeys.Add(builder.Build());
        return this;
    }

    /// <summary>Sets the provider-specific table options.</summary>
    /// <param name="options">The provider-specific table options.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    public TableBuilder WithOptions(TableOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current <see cref="TableBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public TableBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="Table"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="Table"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithQualifiedName(SchemaQualifiedName)"/> or <see cref="WithQualifiedName(string?, string)"/> has not been called.
    /// </exception>
    public Table Build()
    {
        if (_schemaQualifiedName is null)
        {
            throw new InvalidOperationException(
                $"A schema-qualified name is required. Call {nameof(WithQualifiedName)} before {nameof(Build)}.");
        }

        return new Table
        {
            QualifiedName = _schemaQualifiedName.Value,
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
