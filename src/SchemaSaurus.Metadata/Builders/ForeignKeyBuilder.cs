namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="ForeignKey"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class ForeignKeyBuilder : IAnnotationBuilder<ForeignKeyBuilder>
{
    private string? _name;
    private SchemaQualifiedName? _principalTableName;
    private ReferentialAction _onDelete = ReferentialAction.NoAction;
    private ReferentialAction _onUpdate = ReferentialAction.NoAction;
    private bool _isDisabled;
    private readonly List<ForeignKeyColumnMapping> _columnMappings = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the foreign key constraint name.</summary>
    /// <param name="name">The foreign key constraint name.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    public ForeignKeyBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets the principal (referenced) table name.</summary>
    /// <param name="principalTableName">The schema-qualified name of the principal table.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    public ForeignKeyBuilder WithPrincipalTableName(SchemaQualifiedName principalTableName)
    {
        _principalTableName = principalTableName;
        return this;
    }

    /// <summary>Sets the principal (referenced) table name from schema and table name strings.</summary>
    /// <param name="schema">The schema name, or <see langword="null"/> when no schema is provided.</param>
    /// <param name="name">The principal table name.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public ForeignKeyBuilder WithPrincipalTableName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _principalTableName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Adds a column mapping between the dependent and principal tables.</summary>
    /// <param name="dependentColumnName">The dependent table column name.</param>
    /// <param name="principalColumnName">The principal table column name.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dependentColumnName"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="principalColumnName"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public ForeignKeyBuilder AddColumnMapping(string dependentColumnName, string principalColumnName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependentColumnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalColumnName);

        _columnMappings.Add(new ForeignKeyColumnMapping
        {
            DependentColumnName = dependentColumnName,
            PrincipalColumnName = principalColumnName,
        });
        return this;
    }

    /// <summary>Sets the referential action on delete.</summary>
    /// <param name="onDelete">The referential action to apply when the principal row is deleted.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    public ForeignKeyBuilder WithOnDelete(ReferentialAction onDelete)
    {
        _onDelete = onDelete;
        return this;
    }

    /// <summary>Sets the referential action on update.</summary>
    /// <param name="onUpdate">The referential action to apply when the principal row is updated.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    public ForeignKeyBuilder WithOnUpdate(ReferentialAction onUpdate)
    {
        _onUpdate = onUpdate;
        return this;
    }

    /// <summary>Sets whether the foreign key is currently disabled.</summary>
    /// <param name="isDisabled"><see langword="true"/> to mark the foreign key as disabled; otherwise, <see langword="false"/>.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    public ForeignKeyBuilder WithIsDisabled(bool isDisabled)
    {
        _isDisabled = isDisabled;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current <see cref="ForeignKeyBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public ForeignKeyBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="ForeignKey"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="ForeignKey"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithName"/> has not been called.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithPrincipalTableName(SchemaQualifiedName)"/> or <see cref="WithPrincipalTableName(string?, string)"/> has not been called.
    /// </exception>
    public ForeignKey Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException(
                $"A foreign key name is required. Call {nameof(WithName)} before {nameof(Build)}.");
        }

        if (_principalTableName is null)
        {
            throw new InvalidOperationException(
                $"A principal table name is required. Call {nameof(WithPrincipalTableName)} before {nameof(Build)}.");
        }

        return new ForeignKey
        {
            Name = _name!,
            PrincipalTableName = _principalTableName.Value,
            ColumnMappings = _columnMappings,
            OnDelete = _onDelete,
            OnUpdate = _onUpdate,
            IsDisabled = _isDisabled,
            Annotations = _annotations,
        };
    }
}
