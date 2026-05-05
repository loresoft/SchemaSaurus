namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="Column"/> instance,
/// typically populated from a <see cref="System.Data.Common.DbDataReader"/> row.
/// </summary>
public sealed class ColumnBuilder : IAnnotationBuilder<ColumnBuilder>
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
    /// <param name="name">The column name.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Sets the one-based ordinal position of the column.</summary>
    /// <param name="ordinalPosition">The one-based ordinal position.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithOrdinalPosition(int ordinalPosition)
    {
        _ordinalPosition = ordinalPosition;
        return this;
    }

    /// <summary>Sets whether the column accepts null values.</summary>
    /// <param name="isNullable"><see langword="true"/> when the column allows <see langword="null"/> values; otherwise <see langword="false"/>.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsNullable(bool isNullable)
    {
        _isNullable = isNullable;
        return this;
    }

    /// <summary>Sets the default value SQL expression.</summary>
    /// <param name="defaultValueSql">The SQL expression used as the default value, or <see langword="null"/> when no default exists.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithDefaultValueSql(string? defaultValueSql)
    {
        _defaultValueSql = defaultValueSql;
        return this;
    }

    /// <summary>Sets whether this is an identity column.</summary>
    /// <param name="isIdentity"><see langword="true"/> when the column is identity-backed; otherwise <see langword="false"/>.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsIdentity(bool isIdentity)
    {
        _isIdentity = isIdentity;
        return this;
    }

    /// <summary>Sets the identity seed value.</summary>
    /// <param name="identitySeed">The starting value for the identity sequence, or <see langword="null"/> when not applicable.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIdentitySeed(long? identitySeed)
    {
        _identitySeed = identitySeed;
        return this;
    }

    /// <summary>Sets the identity increment value.</summary>
    /// <param name="identityIncrement">The step value for the identity sequence, or <see langword="null"/> when not applicable.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIdentityIncrement(long? identityIncrement)
    {
        _identityIncrement = identityIncrement;
        return this;
    }

    /// <summary>Sets whether this is a computed column.</summary>
    /// <param name="isComputed"><see langword="true"/> when the column value is computed; otherwise <see langword="false"/>.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsComputed(bool isComputed)
    {
        _isComputed = isComputed;
        return this;
    }

    /// <summary>Sets the SQL expression for a computed column.</summary>
    /// <param name="computedColumnSql">The SQL expression for the computed column, or <see langword="null"/> when not applicable.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithComputedColumnSql(string? computedColumnSql)
    {
        _computedColumnSql = computedColumnSql;
        return this;
    }

    /// <summary>Sets whether a computed column is persisted to disk.</summary>
    /// <param name="isStored"><see langword="true"/> when the computed column is persisted; otherwise <see langword="false"/>.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsStored(bool isStored)
    {
        _isStored = isStored;
        return this;
    }

    /// <summary>Sets whether this is a row version / timestamp column.</summary>
    /// <param name="isRowVersion"><see langword="true"/> when the column is a row version; otherwise <see langword="false"/>.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsRowVersion(bool isRowVersion)
    {
        _isRowVersion = isRowVersion;
        return this;
    }

    /// <summary>Sets whether this column is a concurrency token.</summary>
    /// <param name="isConcurrencyToken"><see langword="true"/> when the column participates in optimistic concurrency checks; otherwise <see langword="false"/>.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsConcurrencyToken(bool isConcurrencyToken)
    {
        _isConcurrencyToken = isConcurrencyToken;
        return this;
    }

    /// <summary>Sets the collation override for this column.</summary>
    /// <param name="collation">The collation name, or <see langword="null"/> to use the default collation.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithCollation(string? collation)
    {
        _collation = collation;
        return this;
    }

    /// <summary>Sets the column description or comment.</summary>
    /// <param name="description">The column description, or <see langword="null"/> when no description is available.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the normalized, provider-independent data type.</summary>
    /// <param name="dbType">The normalized database type.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithDbType(DbType dbType)
    {
        _dbType = dbType;
        return this;
    }

    /// <summary>Sets the raw provider type name (e.g., <c>"nvarchar(256)"</c>).</summary>
    /// <param name="nativeTypeName">The provider-specific type name.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithNativeTypeName(string nativeTypeName)
    {
        _nativeTypeName = nativeTypeName;
        return this;
    }

    /// <summary>Sets the .NET CLR type this column maps to.</summary>
    /// <param name="systemType">The CLR type mapped from the provider type.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithSystemType(Type systemType)
    {
        _systemType = systemType;
        return this;
    }

    /// <summary>Sets the maximum character or byte length.</summary>
    /// <param name="maxLength">The max length value, or <see langword="null"/> when not applicable.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithMaxLength(int? maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>Sets the numeric precision.</summary>
    /// <param name="precision">The numeric precision, or <see langword="null"/> when not applicable.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithPrecision(int? precision)
    {
        _precision = precision;
        return this;
    }

    /// <summary>Sets the numeric scale.</summary>
    /// <param name="scale">The numeric scale, or <see langword="null"/> when not applicable.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithScale(int? scale)
    {
        _scale = scale;
        return this;
    }

    /// <summary>Sets whether the string type stores Unicode characters.</summary>
    /// <param name="isUnicode"><see langword="true"/> for Unicode storage, <see langword="false"/> for non-Unicode storage, or <see langword="null"/> when not specified.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsUnicode(bool? isUnicode)
    {
        _isUnicode = isUnicode;
        return this;
    }

    /// <summary>Sets whether the type is fixed-length.</summary>
    /// <param name="isFixedLength"><see langword="true"/> for fixed-length types, <see langword="false"/> for variable-length types, or <see langword="null"/> when not specified.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    public ColumnBuilder WithIsFixedLength(bool? isFixedLength)
    {
        _isFixedLength = isFixedLength;
        return this;
    }

    /// <summary>Adds a provider-specific annotation.</summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current <see cref="ColumnBuilder"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public ColumnBuilder WithAnnotation(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is not null)
            _annotations[key] = value;

        return this;
    }

    /// <summary>
    /// Constructs the immutable <see cref="Column"/> instance.
    /// </summary>
    /// <returns>A fully initialized <see cref="Column"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more required properties have not been set.
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
