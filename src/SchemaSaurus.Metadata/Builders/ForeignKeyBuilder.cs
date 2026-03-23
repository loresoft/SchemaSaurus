namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="ForeignKey"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class ForeignKeyBuilder
{
    private string? _name;
    private SchemaQualifiedName? _principalTableName;
    private ReferentialAction _onDelete = ReferentialAction.NoAction;
    private ReferentialAction _onUpdate = ReferentialAction.NoAction;
    private bool _isDisabled;
    private readonly List<ForeignKeyColumnMapping> _columnMappings = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the foreign key constraint name.</summary>
    public ForeignKeyBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets the principal (referenced) table name.</summary>
    public ForeignKeyBuilder WithPrincipalTableName(SchemaQualifiedName principalTableName)
    {
        _principalTableName = principalTableName;
        return this;
    }

    /// <summary>Sets the principal (referenced) table name from schema and table name strings.</summary>
    public ForeignKeyBuilder WithPrincipalTableName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _principalTableName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Adds a column mapping between the dependent and principal tables.</summary>
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
    public ForeignKeyBuilder WithOnDelete(ReferentialAction onDelete)
    {
        _onDelete = onDelete;
        return this;
    }

    /// <summary>Sets the referential action on update.</summary>
    public ForeignKeyBuilder WithOnUpdate(ReferentialAction onUpdate)
    {
        _onUpdate = onUpdate;
        return this;
    }

    /// <summary>Sets whether the foreign key is currently disabled.</summary>
    public ForeignKeyBuilder WithIsDisabled(bool isDisabled)
    {
        _isDisabled = isDisabled;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public ForeignKeyBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="ForeignKey"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="ForeignKey"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required properties have not been set.
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
            Name = _name,
            PrincipalTableName = _principalTableName.Value,
            ColumnMappings = _columnMappings,
            OnDelete = _onDelete,
            OnUpdate = _onUpdate,
            IsDisabled = _isDisabled,
            Annotations = _annotations,
        };
    }
}
