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
    private string? _description;
    private readonly List<Parameter> _parameters = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the function.</summary>
    /// <param name="name">The schema-qualified function name.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    public ScalarFunctionBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and function name strings.</summary>
    /// <param name="schema">The schema name, or <see langword="null"/> when no schema is provided.</param>
    /// <param name="name">The function name.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public ScalarFunctionBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the return type mapping for the scalar function.</summary>
    /// <param name="returnType">The return type mapping.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="returnType"/> is <see langword="null"/>.</exception>
    public ScalarFunctionBuilder WithReturnType(TypeMapping returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);
        _returnType = returnType;
        return this;
    }

    /// <summary>Sets the return type using individual type facets.</summary>
    /// <param name="dbType">The provider-independent database type.</param>
    /// <param name="nativeTypeName">The provider-specific type name.</param>
    /// <param name="systemType">The CLR <see cref="Type"/> mapped from the database type.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="nativeTypeName"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="systemType"/> is <see langword="null"/>.</exception>
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
    /// <param name="isDeterministic"><see langword="true"/> if deterministic; otherwise, <see langword="false"/>.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    public ScalarFunctionBuilder WithIsDeterministic(bool isDeterministic)
    {
        _isDeterministic = isDeterministic;
        return this;
    }

    /// <summary>Sets the SQL definition of the function.</summary>
    /// <param name="definition">The SQL definition text, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    public ScalarFunctionBuilder WithDefinition(string? definition)
    {
        _definition = definition;
        return this;
    }

    /// <summary>Sets the function description.</summary>
    /// <param name="description">The function description, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    public ScalarFunctionBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Adds a parameter to the function.</summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameter"/> is <see langword="null"/>.</exception>
    public ScalarFunctionBuilder AddParameter(Parameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _parameters.Add(parameter);
        return this;
    }

    /// <summary>Adds a parameter to the function using a builder action.</summary>
    /// <param name="configure">An action that configures a <see cref="ParameterBuilder"/> instance.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public ScalarFunctionBuilder AddParameter(Action<ParameterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ParameterBuilder();
        configure(builder);
        _parameters.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current <see cref="ScalarFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public ScalarFunctionBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="ScalarFunction"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="ScalarFunction"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithSchemaQualifiedName(SchemaQualifiedName)"/> or <see cref="WithSchemaQualifiedName(string?, string)"/> has not been called.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithReturnType(TypeMapping)"/> or <see cref="WithReturnType(DbType, string, Type)"/> has not been called.
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
            Description = _description,
            Parameters = _parameters,
            Annotations = _annotations,
        };
    }
}
