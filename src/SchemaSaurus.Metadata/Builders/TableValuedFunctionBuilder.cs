namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="TableValuedFunction"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class TableValuedFunctionBuilder : IAnnotationBuilder<TableValuedFunctionBuilder>
{
    private SchemaQualifiedName? _schemaQualifiedName;
    private string? _definition;
    private readonly List<Parameter> _parameters = [];
    private readonly List<ReturnColumn> _returnColumns = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the function.</summary>
    public TableValuedFunctionBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and function name strings.</summary>
    public TableValuedFunctionBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the SQL definition of the function.</summary>
    public TableValuedFunctionBuilder WithDefinition(string? definition)
    {
        _definition = definition;
        return this;
    }

    /// <summary>Adds a parameter to the function.</summary>
    public TableValuedFunctionBuilder AddParameter(Parameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _parameters.Add(parameter);
        return this;
    }

    /// <summary>Adds a parameter to the function using a builder action.</summary>
    public TableValuedFunctionBuilder AddParameter(Action<ParameterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ParameterBuilder();
        configure(builder);
        _parameters.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a return column descriptor to the function.</summary>
    public TableValuedFunctionBuilder AddReturnColumn(ReturnColumn returnColumn)
    {
        ArgumentNullException.ThrowIfNull(returnColumn);
        _returnColumns.Add(returnColumn);
        return this;
    }

    /// <summary>Adds a return column descriptor using individual properties.</summary>
    public TableValuedFunctionBuilder AddReturnColumn(
        string name,
        int ordinalPosition,
        DbType dbType,
        string nativeTypeName,
        Type systemType,
        bool isNullable = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeTypeName);
        ArgumentNullException.ThrowIfNull(systemType);
        _returnColumns.Add(new ReturnColumn
        {
            Name = name,
            OrdinalPosition = ordinalPosition,
            IsNullable = isNullable,
            DbType = dbType,
            NativeTypeName = nativeTypeName,
            SystemType = systemType,
        });
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public TableValuedFunctionBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="TableValuedFunction"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="TableValuedFunction"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the schema-qualified name has not been set.
    /// </exception>
    public TableValuedFunction Build()
    {
        if (_schemaQualifiedName is null)
        {
            throw new InvalidOperationException(
                $"A schema-qualified name is required. Call {nameof(WithSchemaQualifiedName)} before {nameof(Build)}.");
        }

        return new TableValuedFunction
        {
            SchemaQualifiedName = _schemaQualifiedName.Value,
            Definition = _definition,
            Parameters = _parameters,
            ReturnColumns = _returnColumns,
            Annotations = _annotations,
        };
    }
}
