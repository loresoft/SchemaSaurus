namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="TableValuedFunction"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class TableValuedFunctionBuilder : IAnnotationBuilder<TableValuedFunctionBuilder>
{
    private SchemaQualifiedName? _schemaQualifiedName;
    private string? _definition;
    private string? _description;
    private readonly List<Parameter> _parameters = [];
    private readonly List<ReturnColumn> _returnColumns = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the function.</summary>
    /// <param name="name">The schema-qualified function name.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    public TableValuedFunctionBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and function name strings.</summary>
    /// <param name="schema">The schema name, or <see langword="null"/> when no schema is provided.</param>
    /// <param name="name">The function name.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public TableValuedFunctionBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the SQL definition of the function.</summary>
    /// <param name="definition">The SQL definition text, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    public TableValuedFunctionBuilder WithDefinition(string? definition)
    {
        _definition = definition;
        return this;
    }

    /// <summary>Sets the function description.</summary>
    /// <param name="description">The function description, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    public TableValuedFunctionBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Adds a parameter to the function.</summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameter"/> is <see langword="null"/>.</exception>
    public TableValuedFunctionBuilder AddParameter(Parameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _parameters.Add(parameter);
        return this;
    }

    /// <summary>Adds a parameter to the function using a builder action.</summary>
    /// <param name="configure">An action that configures a <see cref="ParameterBuilder"/> instance.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public TableValuedFunctionBuilder AddParameter(Action<ParameterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ParameterBuilder();
        configure(builder);
        _parameters.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a return column descriptor to the function.</summary>
    /// <param name="returnColumn">The return column descriptor to add.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="returnColumn"/> is <see langword="null"/>.</exception>
    public TableValuedFunctionBuilder AddReturnColumn(ReturnColumn returnColumn)
    {
        ArgumentNullException.ThrowIfNull(returnColumn);
        _returnColumns.Add(returnColumn);
        return this;
    }

    /// <summary>Adds a return column descriptor using individual properties.</summary>
    /// <param name="name">The return column name.</param>
    /// <param name="ordinalPosition">The zero-based ordinal position of the return column.</param>
    /// <param name="dbType">The provider-independent database type.</param>
    /// <param name="nativeTypeName">The provider-specific type name.</param>
    /// <param name="systemType">The CLR <see cref="Type"/> mapped from the database type.</param>
    /// <param name="isNullable"><see langword="true"/> if nullable; otherwise, <see langword="false"/>.</param>
    /// <param name="maxLength">The max length value, or <see langword="null"/> to leave it unspecified.</param>
    /// <param name="precision">The precision value, or <see langword="null"/> to leave it unspecified.</param>
    /// <param name="scale">The scale value, or <see langword="null"/> to leave it unspecified.</param>
    /// <param name="isUnicode"><see langword="true"/> for Unicode storage, <see langword="false"/> for non-Unicode, or <see langword="null"/> when unspecified.</param>
    /// <param name="isFixedLength"><see langword="true"/> for fixed-length, <see langword="false"/> for variable-length, or <see langword="null"/> when unspecified.</param>
    /// <param name="annotations">Optional provider-specific annotations for the return column.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="nativeTypeName"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="systemType"/> is <see langword="null"/>.</exception>
    public TableValuedFunctionBuilder AddReturnColumn(
        string name,
        int ordinalPosition,
        DbType dbType,
        string nativeTypeName,
        Type systemType,
        bool isNullable = false,
        int? maxLength = null,
        int? precision = null,
        int? scale = null,
        bool? isUnicode = null,
        bool? isFixedLength = null,
        IReadOnlyDictionary<string, object?>? annotations = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeTypeName);
        ArgumentNullException.ThrowIfNull(systemType);

        ReturnColumn returnColumn = new()
        {
            Name = name,
            OrdinalPosition = ordinalPosition,
            IsNullable = isNullable,
            DbType = dbType,
            NativeTypeName = nativeTypeName,
            SystemType = systemType,
            MaxLength = maxLength,
            Precision = precision,
            Scale = scale,
            IsUnicode = isUnicode,
            IsFixedLength = isFixedLength,
            Annotations = annotations ?? new Dictionary<string, object?>(),
        };

        _returnColumns.Add(returnColumn);
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current <see cref="TableValuedFunctionBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public TableValuedFunctionBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="TableValuedFunction"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="TableValuedFunction"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithSchemaQualifiedName(SchemaQualifiedName)"/> or <see cref="WithSchemaQualifiedName(string?, string)"/> has not been called.
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
            Description = _description,
            Parameters = _parameters,
            ReturnColumns = _returnColumns,
            Annotations = _annotations,
        };
    }
}
