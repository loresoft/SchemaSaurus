namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="ScalarFunction"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class ScalarFunctionBuilder : IAnnotationBuilder<ScalarFunctionBuilder>
{
    private SchemaQualifiedName? _schemaQualifiedName;
    private TypeMapping? _returnType;
    private bool _isDeterministic;
    private string? _definition;
    private readonly List<Parameter> _parameters = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the function.</summary>
    public ScalarFunctionBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and function name strings.</summary>
    public ScalarFunctionBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the return type mapping for the scalar function.</summary>
    public ScalarFunctionBuilder WithReturnType(TypeMapping returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);
        _returnType = returnType;
        return this;
    }

    /// <summary>Sets the return type using individual type facets.</summary>
    public ScalarFunctionBuilder WithReturnType(DbType dbType, string nativeTypeName, Type systemType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeTypeName);
        ArgumentNullException.ThrowIfNull(systemType);
        _returnType = new TypeMapping
        {
            DbType = dbType,
            NativeTypeName = nativeTypeName,
            SystemType = systemType,
        };
        return this;
    }

    /// <summary>Sets whether the function is deterministic.</summary>
    public ScalarFunctionBuilder WithIsDeterministic(bool isDeterministic)
    {
        _isDeterministic = isDeterministic;
        return this;
    }

    /// <summary>Sets the SQL definition of the function.</summary>
    public ScalarFunctionBuilder WithDefinition(string? definition)
    {
        _definition = definition;
        return this;
    }

    /// <summary>Adds a parameter to the function.</summary>
    public ScalarFunctionBuilder AddParameter(Parameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _parameters.Add(parameter);
        return this;
    }

    /// <summary>Adds a parameter to the function using a builder action.</summary>
    public ScalarFunctionBuilder AddParameter(Action<ParameterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ParameterBuilder();
        configure(builder);
        _parameters.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public ScalarFunctionBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="ScalarFunction"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="ScalarFunction"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required properties have not been set.
    /// </exception>
    public ScalarFunction Build()
    {
        if (_schemaQualifiedName is null)
        {
            throw new InvalidOperationException(
                $"A schema-qualified name is required. Call {nameof(WithSchemaQualifiedName)} before {nameof(Build)}.");
        }

        if (_returnType is null)
        {
            throw new InvalidOperationException(
                $"A return type is required. Call {nameof(WithReturnType)} before {nameof(Build)}.");
        }

        return new ScalarFunction
        {
            SchemaQualifiedName = _schemaQualifiedName.Value,
            ReturnType = _returnType,
            IsDeterministic = _isDeterministic,
            Definition = _definition,
            Parameters = _parameters,
            Annotations = _annotations,
        };
    }
}
