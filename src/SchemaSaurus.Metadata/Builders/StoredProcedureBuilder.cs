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
    public StoredProcedureBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and procedure name strings.</summary>
    public StoredProcedureBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the SQL definition of the stored procedure.</summary>
    public StoredProcedureBuilder WithDefinition(string? definition)
    {
        _definition = definition;
        return this;
    }

    /// <summary>Sets the stored procedure description or comment.</summary>
    public StoredProcedureBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Adds a parameter to the stored procedure.</summary>
    public StoredProcedureBuilder AddParameter(Parameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _parameters.Add(parameter);
        return this;
    }

    /// <summary>Adds a parameter to the stored procedure using a builder action.</summary>
    public StoredProcedureBuilder AddParameter(Action<ParameterBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ParameterBuilder();
        configure(builder);
        _parameters.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
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
    /// Thrown when the schema-qualified name has not been set.
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
