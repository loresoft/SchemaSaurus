namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for incrementally constructing an immutable <see cref="DatabaseModel"/>.
/// Objects are added to the builder directly — there is no intermediate schema builder,
/// because schema membership is derived from each object's <see cref="SchemaQualifiedName"/>.
/// Call <see cref="Build"/> to produce the frozen model with all parent back-references
/// wired.
/// </summary>
public sealed class DatabaseModelBuilder : IAnnotationBuilder<DatabaseModelBuilder>
{
    private string? _databaseName;
    private string? _collation;
    private string? _edition;
    private string? _engineEdition;
    private string? _compatibilityLevel;
    private string? _defaultSchemaName;
    private string? _provider;
    private string? _serverVersion;
    private readonly List<Table> _tables = [];
    private readonly List<View> _views = [];
    private readonly List<Sequence> _sequences = [];
    private readonly List<StoredProcedure> _storedProcedures = [];
    private readonly List<ScalarFunction> _scalarFunctions = [];
    private readonly List<TableValuedFunction> _tableValuedFunctions = [];
    private readonly List<UserDefinedType> _userDefinedTypes = [];
    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the database name.</summary>
    public DatabaseModelBuilder WithDatabaseName(string name)
    {
        _databaseName = name;
        return this;
    }

    /// <summary>Sets the default collation name.</summary>
    public DatabaseModelBuilder WithCollation(string? collation)
    {
        _collation = collation;
        return this;
    }

    /// <summary>Sets the database engine edition or product variant.</summary>
    public DatabaseModelBuilder WithEdition(string? edition)
    {
        _edition = edition;
        return this;
    }

    /// <summary>Sets the database engine edition family or deployment type.</summary>
    public DatabaseModelBuilder WithEngineEdition(string? engineEdition)
    {
        _engineEdition = engineEdition;
        return this;
    }

    /// <summary>Sets the database compatibility level.</summary>
    public DatabaseModelBuilder WithCompatibilityLevel(string? compatibilityLevel)
    {
        _compatibilityLevel = compatibilityLevel;
        return this;
    }

    /// <summary>Sets the default schema name (e.g., <c>"dbo"</c>, <c>"public"</c>).</summary>
    public DatabaseModelBuilder WithDefaultSchemaName(string? schemaName)
    {
        _defaultSchemaName = schemaName;
        return this;
    }

    /// <summary>Sets the database provider identifier.</summary>
    public DatabaseModelBuilder WithProvider(string provider)
    {
        _provider = provider;
        return this;
    }

    /// <summary>Sets the server version string.</summary>
    public DatabaseModelBuilder WithServerVersion(string? serverVersion)
    {
        _serverVersion = serverVersion;
        return this;
    }


    /// <summary>Adds a table to the model.</summary>
    public DatabaseModelBuilder AddTable(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);

        _tables.Add(table);
        return this;
    }

    /// <summary>Adds a table to the model using a builder action.</summary>
    public DatabaseModelBuilder AddTable(Action<TableBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableBuilder();
        configure(builder);

        _tables.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a view to the model.</summary>
    public DatabaseModelBuilder AddView(View view)
    {
        ArgumentNullException.ThrowIfNull(view);

        _views.Add(view);
        return this;
    }

    /// <summary>Adds a view to the model using a builder action.</summary>
    public DatabaseModelBuilder AddView(Action<ViewBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ViewBuilder();
        configure(builder);

        _views.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a sequence to the model.</summary>
    public DatabaseModelBuilder AddSequence(Sequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence);

        _sequences.Add(sequence);
        return this;
    }

    /// <summary>Adds a sequence to the model using a builder action.</summary>
    public DatabaseModelBuilder AddSequence(Action<SequenceBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new SequenceBuilder();
        configure(builder);

        _sequences.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a stored procedure to the model.</summary>
    public DatabaseModelBuilder AddStoredProcedure(StoredProcedure storedProcedure)
    {
        ArgumentNullException.ThrowIfNull(storedProcedure);

        _storedProcedures.Add(storedProcedure);
        return this;
    }

    /// <summary>Adds a stored procedure to the model using a builder action.</summary>
    public DatabaseModelBuilder AddStoredProcedure(Action<StoredProcedureBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new StoredProcedureBuilder();
        configure(builder);

        _storedProcedures.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a scalar function to the model.</summary>
    public DatabaseModelBuilder AddScalarFunction(ScalarFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);

        _scalarFunctions.Add(function);
        return this;
    }

    /// <summary>Adds a scalar function to the model using a builder action.</summary>
    public DatabaseModelBuilder AddScalarFunction(Action<ScalarFunctionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ScalarFunctionBuilder();
        configure(builder);

        _scalarFunctions.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a table-valued function to the model.</summary>
    public DatabaseModelBuilder AddTableValuedFunction(TableValuedFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);

        _tableValuedFunctions.Add(function);
        return this;
    }

    /// <summary>Adds a table-valued function to the model using a builder action.</summary>
    public DatabaseModelBuilder AddTableValuedFunction(Action<TableValuedFunctionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new TableValuedFunctionBuilder();
        configure(builder);

        _tableValuedFunctions.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a user-defined type to the model.</summary>
    public DatabaseModelBuilder AddUserDefinedType(UserDefinedType userDefinedType)
    {
        ArgumentNullException.ThrowIfNull(userDefinedType);

        _userDefinedTypes.Add(userDefinedType);
        return this;
    }

    /// <summary>Adds a user-defined type to the model using a builder action.</summary>
    public DatabaseModelBuilder AddUserDefinedType(Action<UserDefinedTypeBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new UserDefinedTypeBuilder();
        configure(builder);

        _userDefinedTypes.Add(builder.Build());
        return this;
    }


    /// <summary>Adds a provider-specific annotation to the root <see cref="DatabaseModel"/>.</summary>
    public DatabaseModelBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }


    /// <summary>
    /// Constructs the immutable <see cref="DatabaseModel"/>, wires all parent back-references.
    /// </summary>
    /// <returns>A fully initialized, immutable <see cref="DatabaseModel"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithDatabaseName"/> has not been called.
    /// </exception>
    public DatabaseModel Build()
    {
        if (string.IsNullOrWhiteSpace(_databaseName))
        {
            throw new InvalidOperationException(
                $"A database name is required. Call {nameof(WithDatabaseName)} before {nameof(Build)}.");
        }

        if (string.IsNullOrWhiteSpace(_provider))
        {
            throw new InvalidOperationException(
                $"A provider is required. Call {nameof(WithProvider)} before {nameof(Build)}.");
        }

        var model = new DatabaseModel
        {
            DatabaseName = _databaseName!,
            Collation = _collation,
            Edition = _edition,
            EngineEdition = _engineEdition,
            CompatibilityLevel = _compatibilityLevel,
            DefaultSchemaName = _defaultSchemaName,
            Provider = _provider!,
            ServerVersion = _serverVersion,
            Tables = _tables,
            Views = _views,
            Sequences = _sequences,
            StoredProcedures = _storedProcedures,
            ScalarFunctions = _scalarFunctions,
            TableValuedFunctions = _tableValuedFunctions,
            UserDefinedTypes = _userDefinedTypes,
            Annotations = _annotations,
        };

        model.ResolveReferences();
        return model;
    }
}
