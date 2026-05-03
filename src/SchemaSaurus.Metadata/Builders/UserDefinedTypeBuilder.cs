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
    public UserDefinedTypeBuilder WithSchemaQualifiedName(SchemaQualifiedName name)
    {
        _schemaQualifiedName = name;
        return this;
    }

    /// <summary>Sets the schema-qualified name from schema and type name strings.</summary>
    public UserDefinedTypeBuilder WithSchemaQualifiedName(string? schema, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _schemaQualifiedName = new SchemaQualifiedName { Schema = schema, Name = name };
        return this;
    }

    /// <summary>Sets the structural kind of this user-defined type.</summary>
    public UserDefinedTypeBuilder WithKind(UserDefinedTypeKind kind)
    {
        _kind = kind;
        return this;
    }

    /// <summary>Adds a column for table-type user-defined types.</summary>
    public UserDefinedTypeBuilder AddColumn(Column column)
    {
        ArgumentNullException.ThrowIfNull(column);
        _columns ??= [];
        _columns.Add(column);
        return this;
    }

    /// <summary>Adds a column for table-type user-defined types using a builder action.</summary>
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
    public UserDefinedTypeBuilder AddEnumLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        _enumLabels ??= [];
        _enumLabels.Add(label);
        return this;
    }

    /// <summary>Sets the normalized, provider-independent data type.</summary>
    public UserDefinedTypeBuilder WithDbType(DbType dbType)
    {
        _dbType = dbType;
        return this;
    }

    /// <summary>Sets the raw provider type name.</summary>
    public UserDefinedTypeBuilder WithNativeTypeName(string nativeTypeName)
    {
        _nativeTypeName = nativeTypeName;
        return this;
    }

    /// <summary>Sets the .NET CLR type this type maps to.</summary>
    public UserDefinedTypeBuilder WithSystemType(Type systemType)
    {
        _systemType = systemType;
        return this;
    }

    /// <summary>Sets the maximum character or byte length.</summary>
    public UserDefinedTypeBuilder WithMaxLength(int? maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>Sets the numeric precision.</summary>
    public UserDefinedTypeBuilder WithPrecision(int? precision)
    {
        _precision = precision;
        return this;
    }

    /// <summary>Sets the numeric scale.</summary>
    public UserDefinedTypeBuilder WithScale(int? scale)
    {
        _scale = scale;
        return this;
    }

    /// <summary>Sets whether the string type stores Unicode characters.</summary>
    public UserDefinedTypeBuilder WithIsUnicode(bool? isUnicode)
    {
        _isUnicode = isUnicode;
        return this;
    }

    /// <summary>Sets whether the type is fixed-length.</summary>
    public UserDefinedTypeBuilder WithIsFixedLength(bool? isFixedLength)
    {
        _isFixedLength = isFixedLength;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public UserDefinedTypeBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="UserDefinedType"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="UserDefinedType"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required properties have not been set.
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
