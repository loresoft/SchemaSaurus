namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="Parameter"/> instance,
/// typically populated from a <see cref="System.Data.Common.DbDataReader"/> row.
/// </summary>
public sealed class ParameterBuilder : IAnnotationBuilder<ParameterBuilder>
{
    private string? _name;
    private int? _ordinal;
    private ParameterDirection _direction = ParameterDirection.Input;
    private string? _defaultValueSql;

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

    /// <summary>Sets the parameter name.</summary>
    public ParameterBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets the one-based ordinal position.</summary>
    public ParameterBuilder WithOrdinal(int ordinal)
    {
        _ordinal = ordinal;
        return this;
    }

    /// <summary>Sets the parameter direction.</summary>
    public ParameterBuilder WithDirection(ParameterDirection direction)
    {
        _direction = direction;
        return this;
    }

    /// <summary>Sets the default value SQL expression.</summary>
    public ParameterBuilder WithDefaultValueSql(string? defaultValueSql)
    {
        _defaultValueSql = defaultValueSql;
        return this;
    }

    /// <summary>Sets the normalized, provider-independent data type.</summary>
    public ParameterBuilder WithDbType(DbType dbType)
    {
        _dbType = dbType;
        return this;
    }

    /// <summary>Sets the raw provider type name.</summary>
    public ParameterBuilder WithNativeTypeName(string nativeTypeName)
    {
        _nativeTypeName = nativeTypeName;
        return this;
    }

    /// <summary>Sets the .NET CLR type this parameter maps to.</summary>
    public ParameterBuilder WithSystemType(Type systemType)
    {
        _systemType = systemType;
        return this;
    }

    /// <summary>Sets the maximum character or byte length.</summary>
    public ParameterBuilder WithMaxLength(int? maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>Sets the numeric precision.</summary>
    public ParameterBuilder WithPrecision(int? precision)
    {
        _precision = precision;
        return this;
    }

    /// <summary>Sets the numeric scale.</summary>
    public ParameterBuilder WithScale(int? scale)
    {
        _scale = scale;
        return this;
    }

    /// <summary>Sets whether the string type stores Unicode characters.</summary>
    public ParameterBuilder WithIsUnicode(bool? isUnicode)
    {
        _isUnicode = isUnicode;
        return this;
    }

    /// <summary>Sets whether the type is fixed-length.</summary>
    public ParameterBuilder WithIsFixedLength(bool? isFixedLength)
    {
        _isFixedLength = isFixedLength;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public ParameterBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="Parameter"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="Parameter"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required properties have not been set.
    /// </exception>
    public Parameter Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException(
                $"A parameter name is required. Call {nameof(WithName)} before {nameof(Build)}.");
        }

        if (_ordinal is null)
        {
            throw new InvalidOperationException(
                $"An ordinal is required. Call {nameof(WithOrdinal)} before {nameof(Build)}.");
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

        return new Parameter
        {
            Name = _name!,
            Ordinal = _ordinal.Value,
            Direction = _direction,
            DefaultValueSql = _defaultValueSql,
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
