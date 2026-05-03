namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="Column"/> instance,
/// typically populated from a <see cref="System.Data.Common.DbDataReader"/> row.
/// </summary>
public sealed class ColumnBuilder
{
    private string? _name;
    private int? _ordinalPosition;
    private bool? _isNullable;
    private string? _defaultValueSql;
    private bool _isIdentity;
    private long? _identitySeed;
    private long? _identityIncrement;
    private bool _isComputed;
    private string? _computedColumnSql;
    private bool _isStored;
    private bool _isRowVersion;
    private bool _isConcurrencyToken;
    private string? _collation;
    private string? _description;

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

    /// <summary>Sets the column name.</summary>
    public ColumnBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets the one-based ordinal position of the column.</summary>
    public ColumnBuilder WithOrdinalPosition(int ordinalPosition)
    {
        _ordinalPosition = ordinalPosition;
        return this;
    }

    /// <summary>Sets whether the column accepts null values.</summary>
    public ColumnBuilder WithIsNullable(bool isNullable)
    {
        _isNullable = isNullable;
        return this;
    }

    /// <summary>Sets the default value SQL expression.</summary>
    public ColumnBuilder WithDefaultValueSql(string? defaultValueSql)
    {
        _defaultValueSql = defaultValueSql;
        return this;
    }

    /// <summary>Sets whether this is an identity column.</summary>
    public ColumnBuilder WithIsIdentity(bool isIdentity)
    {
        _isIdentity = isIdentity;
        return this;
    }

    /// <summary>Sets the identity seed value.</summary>
    public ColumnBuilder WithIdentitySeed(long? identitySeed)
    {
        _identitySeed = identitySeed;
        return this;
    }

    /// <summary>Sets the identity increment value.</summary>
    public ColumnBuilder WithIdentityIncrement(long? identityIncrement)
    {
        _identityIncrement = identityIncrement;
        return this;
    }

    /// <summary>Sets whether this is a computed column.</summary>
    public ColumnBuilder WithIsComputed(bool isComputed)
    {
        _isComputed = isComputed;
        return this;
    }

    /// <summary>Sets the SQL expression for a computed column.</summary>
    public ColumnBuilder WithComputedColumnSql(string? computedColumnSql)
    {
        _computedColumnSql = computedColumnSql;
        return this;
    }

    /// <summary>Sets whether a computed column is persisted to disk.</summary>
    public ColumnBuilder WithIsStored(bool isStored)
    {
        _isStored = isStored;
        return this;
    }

    /// <summary>Sets whether this is a row version / timestamp column.</summary>
    public ColumnBuilder WithIsRowVersion(bool isRowVersion)
    {
        _isRowVersion = isRowVersion;
        return this;
    }

    /// <summary>Sets whether this column is a concurrency token.</summary>
    public ColumnBuilder WithIsConcurrencyToken(bool isConcurrencyToken)
    {
        _isConcurrencyToken = isConcurrencyToken;
        return this;
    }

    /// <summary>Sets the collation override for this column.</summary>
    public ColumnBuilder WithCollation(string? collation)
    {
        _collation = collation;
        return this;
    }

    /// <summary>Sets the column description or comment.</summary>
    public ColumnBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the normalized, provider-independent data type.</summary>
    public ColumnBuilder WithDbType(DbType dbType)
    {
        _dbType = dbType;
        return this;
    }

    /// <summary>Sets the raw provider type name (e.g., <c>"nvarchar(256)"</c>).</summary>
    public ColumnBuilder WithNativeTypeName(string nativeTypeName)
    {
        _nativeTypeName = nativeTypeName;
        return this;
    }

    /// <summary>Sets the .NET CLR type this column maps to.</summary>
    public ColumnBuilder WithSystemType(Type systemType)
    {
        _systemType = systemType;
        return this;
    }

    /// <summary>Sets the maximum character or byte length.</summary>
    public ColumnBuilder WithMaxLength(int? maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>Sets the numeric precision.</summary>
    public ColumnBuilder WithPrecision(int? precision)
    {
        _precision = precision;
        return this;
    }

    /// <summary>Sets the numeric scale.</summary>
    public ColumnBuilder WithScale(int? scale)
    {
        _scale = scale;
        return this;
    }

    /// <summary>Sets whether the string type stores Unicode characters.</summary>
    public ColumnBuilder WithIsUnicode(bool? isUnicode)
    {
        _isUnicode = isUnicode;
        return this;
    }

    /// <summary>Sets whether the type is fixed-length.</summary>
    public ColumnBuilder WithIsFixedLength(bool? isFixedLength)
    {
        _isFixedLength = isFixedLength;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    public ColumnBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _annotations[key] = value;
        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="Column"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="Column"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required properties have not been set.
    /// </exception>
    public Column Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException(
                $"A column name is required. Call {nameof(WithName)} before {nameof(Build)}.");
        }

        if (_ordinalPosition is null)
        {
            throw new InvalidOperationException(
                $"An ordinal position is required. Call {nameof(WithOrdinalPosition)} before {nameof(Build)}.");
        }

        if (_isNullable is null)
        {
            throw new InvalidOperationException(
                $"Nullability is required. Call {nameof(WithIsNullable)} before {nameof(Build)}.");
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

        return new Column
        {
            Name = _name!,
            OrdinalPosition = _ordinalPosition.Value,
            IsNullable = _isNullable.Value,
            DefaultValueSql = _defaultValueSql,
            IsIdentity = _isIdentity,
            IdentitySeed = _identitySeed,
            IdentityIncrement = _identityIncrement,
            IsComputed = _isComputed,
            ComputedColumnSql = _computedColumnSql,
            IsStored = _isStored,
            IsRowVersion = _isRowVersion,
            IsConcurrencyToken = _isConcurrencyToken,
            Collation = _collation,
            Description = _description,
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
