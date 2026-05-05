namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="UserDefinedType"/> instance,
/// typically populated from <see cref="System.Data.Common.DbDataReader"/> rows.
/// </summary>
public sealed class UserDefinedTypeBuilder : IAnnotationBuilder<UserDefinedTypeBuilder>
{
    private SchemaQualifiedName? _schemaQualifiedName;
    private UserDefinedTypeKind? _kind;
    private List<Column>? _columns;
    private List<string>? _enumLabels;

    // TypeMapping fields
    private DbType? _dbType;
    private string? _nativeTypeName;
    private Type? _systemType;
    private int? _maxLength;
    private int? _precision;
    private int? _scale;
    private bool? _isUnicode;
    private bool? _isFixedLength;

    private readonly Dictionary<string, object?> _annotations = [];

    /// <summary>Sets the schema-qualified name of the user-defined type.</summary>
    /// <param name="name">The schema-qualified user-defined type name.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and type name strings.</summary>
    /// <param name="schema">The schema name, or <see langword="null"/> when no schema is provided.</param>
    /// <param name="name">The type name.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public UserDefinedTypeBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the structural kind of this user-defined type.</summary>
    /// <param name="kind">The structural kind.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithKind(UserDefinedTypeKind kind)
    {
        _kind = kind;
        return this;
    }

    /// <summary>Adds a column for table-type user-defined types.</summary>
    /// <param name="column">The column to add.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="column"/> is <see langword="null"/>.</exception>
    public UserDefinedTypeBuilder AddColumn(Column column)
    {
        ArgumentNullException.ThrowIfNull(column);
        _columns ??= [];
        _columns.Add(column);
        return this;
    }

    /// <summary>Adds a column for table-type user-defined types using a builder action.</summary>
    /// <param name="configure">An action that configures a <see cref="ColumnBuilder"/> instance.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    public UserDefinedTypeBuilder AddColumn(Action<ColumnBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ColumnBuilder();
        configure(builder);
        _columns ??= [];
        _columns.Add(builder.Build());
        return this;
    }

    /// <summary>Adds an enum label for enum-type user-defined types.</summary>
    /// <param name="label">The enum label to add.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="label"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public UserDefinedTypeBuilder AddEnumLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        _enumLabels ??= [];
        _enumLabels.Add(label);
        return this;
    }

    /// <summary>Sets the normalized, provider-independent data type.</summary>
    /// <param name="dbType">The provider-independent database type.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithDbType(DbType dbType)
    {
        _dbType = dbType;
        return this;
    }

    /// <summary>Sets the raw provider type name.</summary>
    /// <param name="nativeTypeName">The provider-specific type name.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithNativeTypeName(string nativeTypeName)
    {
        _nativeTypeName = nativeTypeName;
        return this;
    }

    /// <summary>Sets the .NET CLR type this type maps to.</summary>
    /// <param name="systemType">The CLR <see cref="Type"/> mapped from the database type.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithSystemType(Type systemType)
    {
        _systemType = systemType;
        return this;
    }

    /// <summary>Sets the maximum character or byte length.</summary>
    /// <param name="maxLength">The max length value, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithMaxLength(int? maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>Sets the numeric precision.</summary>
    /// <param name="precision">The precision value, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithPrecision(int? precision)
    {
        _precision = precision;
        return this;
    }

    /// <summary>Sets the numeric scale.</summary>
    /// <param name="scale">The scale value, or <see langword="null"/> to leave it unspecified.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithScale(int? scale)
    {
        _scale = scale;
        return this;
    }

    /// <summary>Sets whether the string type stores Unicode characters.</summary>
    /// <param name="isUnicode"><see langword="true"/> for Unicode storage, <see langword="false"/> for non-Unicode, or <see langword="null"/> when unspecified.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithIsUnicode(bool? isUnicode)
    {
        _isUnicode = isUnicode;
        return this;
    }

    /// <summary>Sets whether the type is fixed-length.</summary>
    /// <param name="isFixedLength"><see langword="true"/> for fixed-length, <see langword="false"/> for variable-length, or <see langword="null"/> when unspecified.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    public UserDefinedTypeBuilder WithIsFixedLength(bool? isFixedLength)
    {
        _isFixedLength = isFixedLength;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current <see cref="UserDefinedTypeBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public UserDefinedTypeBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="UserDefinedType"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="UserDefinedType"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithSchemaQualifiedName(SchemaQualifiedName)"/> or <see cref="WithSchemaQualifiedName(string?, string)"/> has not been called.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithKind"/> has not been called.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithDbType"/> has not been called.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithNativeTypeName"/> has not been called.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithSystemType"/> has not been called.
    /// </exception>
    public UserDefinedType Build()
    {
        if (_schemaQualifiedName is null)
        {
            throw new InvalidOperationException(
                $"A schema-qualified name is required. Call {nameof(WithSchemaQualifiedName)} before {nameof(Build)}.");
        }

        if (_kind is null)
        {
            throw new InvalidOperationException(
                $"A kind is required. Call {nameof(WithKind)} before {nameof(Build)}.");
        }

        if (_dbType is null)
        {
            throw new InvalidOperationException(
                $"A DbType is required. Call {nameof(WithDbType)} before {nameof(Build)}.");
        }

        if (string.IsNullOrWhiteSpace(_nativeTypeName))
        {
            throw new InvalidOperationException(
                $"A native type name is required. Call {nameof(WithNativeTypeName)} before {nameof(Build)}.");
        }

        if (_systemType is null)
        {
            throw new InvalidOperationException(
                $"A system type is required. Call {nameof(WithSystemType)} before {nameof(Build)}.");
        }

        return new UserDefinedType
        {
            SchemaQualifiedName = _schemaQualifiedName.Value,
            Kind = _kind.Value,
            Columns = _columns,
            EnumLabels = _enumLabels,
            DbType = _dbType.Value,
            NativeTypeName = _nativeTypeName!,
            SystemType = _systemType,
            MaxLength = _maxLength,
            Precision = _precision,
            Scale = _scale,
            IsUnicode = _isUnicode,
            IsFixedLength = _isFixedLength,
            Annotations = _annotations,
        };
    }
}
