namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="StoredProcedure"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class StoredProcedureBuilder : IAnnotationBuilder<StoredProcedureBuilder>
{
    private SchemaQualifiedName? _schemaQualifiedName;
    private string? _definition;
    private string? _description;
    private readonly List<Parameter> _parameters = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the stored procedure.</summary>
    /// <param name="name">The schema-qualified stored procedure name.</param>
    /// <returns>The current <see cref="StoredProcedureBuilder"/> instance.</returns>
    public StoredProcedureBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and procedure name strings.</summary>
    /// <param name="schema">The schema name, or <see langword="null"/> when no schema is provided.</param>
    /// <param name="name">The stored procedure name.</param>
    /// <returns>The current <see cref="StoredProcedureBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public StoredProcedureBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the SQL definition of the stored procedure.</summary>
    /// <param name="definition">The SQL definition text, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="StoredProcedureBuilder"/> instance.</returns>
    public StoredProcedureBuilder WithDefinition(string? definition)
    {
        _definition = definition;
        return this;
    }

    /// <summary>Sets the stored procedure description or comment.</summary>
    /// <param name="description">The description text, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="StoredProcedureBuilder"/> instance.</returns>
    public StoredProcedureBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Adds a parameter to the stored procedure.</summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <returns>The current <see cref="StoredProcedureBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameter"/> is <see langword="null"/>.</exception>
    public StoredProcedureBuilder AddParameter(Parameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _parameters.Add(parameter);
        return this;
    }

    /// <summary>Adds a parameter to the stored procedure using a builder action.</summary>
    /// <param name="configure">An action that configures a <see cref="ParameterBuilder"/> instance.</param>
    /// <returns>The current <see cref="StoredProcedureBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public StoredProcedureBuilder AddParameter(Action<ParameterBuilder> configure)
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
    /// <returns>The current <see cref="StoredProcedureBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public StoredProcedureBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="StoredProcedure"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="StoredProcedure"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithSchemaQualifiedName(SchemaQualifiedName)"/> or <see cref="WithSchemaQualifiedName(string?, string)"/> has not been called.
    /// </exception>
    public StoredProcedure Build()
    {
        if (_schemaQualifiedName is null)
        {
            throw new InvalidOperationException(
                $"A schema-qualified name is required. Call {nameof(WithSchemaQualifiedName)} before {nameof(Build)}.");
        }

        return new StoredProcedure
        {
            SchemaQualifiedName = _schemaQualifiedName.Value,
            Definition = _definition,
            Description = _description,
            Parameters = _parameters,
            Annotations = _annotations,
        };
    }
}
