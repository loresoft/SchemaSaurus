using System.Collections.Frozen;
using System.Text.Json;

namespace SchemaSaurus.Metadata.Provider;

/// <summary>
/// Provides bidirectional mapping between <see cref="DbType"/> and CLR <see cref="Type"/>.
/// </summary>
/// <remarks>
/// Lookups are backed by <see cref="FrozenDictionary{TKey, TValue}"/> instances
/// that are built once at startup for optimal read performance.
/// When multiple <see cref="DbType"/> values map to the same CLR type (e.g.
/// <see cref="DbType.AnsiString"/> and <see cref="DbType.String"/> both map to
/// <see cref="string"/>), <see cref="ToDbType"/> returns the most commonly used variant.
/// </remarks>
public static class DataTypeMapper
{
    private static readonly FrozenDictionary<DbType, Type> _dbTypeToSystem
        = new Dictionary<DbType, Type>
        {
            [DbType.AnsiString] = typeof(string),
            [DbType.AnsiStringFixedLength] = typeof(string),
            [DbType.Binary] = typeof(byte[]),
            [DbType.Boolean] = typeof(bool),
            [DbType.Byte] = typeof(byte),
            [DbType.Currency] = typeof(decimal),
            [DbType.DateTime] = typeof(DateTime),
            [DbType.DateTime2] = typeof(DateTime),
            [DbType.DateTimeOffset] = typeof(DateTimeOffset),
            [DbType.Decimal] = typeof(decimal),
            [DbType.Double] = typeof(double),
            [DbType.Guid] = typeof(Guid),
            [DbType.Int16] = typeof(short),
            [DbType.Int32] = typeof(int),
            [DbType.Int64] = typeof(long),
            [DbType.Object] = typeof(object),
            [DbType.SByte] = typeof(sbyte),
            [DbType.Single] = typeof(float),
            [DbType.String] = typeof(string),
            [DbType.StringFixedLength] = typeof(string),
            [DbType.UInt16] = typeof(ushort),
            [DbType.UInt32] = typeof(uint),
            [DbType.UInt64] = typeof(ulong),
            [DbType.VarNumeric] = typeof(decimal),
            [DbType.Xml] = typeof(string),
    #if NET6_0_OR_GREATER
            [DbType.Date] = typeof(DateOnly),
            [DbType.Time] = typeof(TimeOnly),
    #else
            [DbType.Date] = typeof(DateTime),
            [DbType.Time] = typeof(TimeSpan),
    #endif
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<Type, DbType> _systemToDbType = new Dictionary<Type, DbType>
    {
        [typeof(bool)] = DbType.Boolean,
        [typeof(byte)] = DbType.Byte,
        [typeof(sbyte)] = DbType.SByte,
        [typeof(short)] = DbType.Int16,
        [typeof(ushort)] = DbType.UInt16,
        [typeof(int)] = DbType.Int32,
        [typeof(uint)] = DbType.UInt32,
        [typeof(long)] = DbType.Int64,
        [typeof(ulong)] = DbType.UInt64,
        [typeof(float)] = DbType.Single,
        [typeof(double)] = DbType.Double,
        [typeof(decimal)] = DbType.Decimal,
        [typeof(string)] = DbType.String,
        [typeof(char)] = DbType.StringFixedLength,
        [typeof(Uri)] = DbType.String,
        [typeof(byte[])] = DbType.Binary,
        [typeof(DateTime)] = DbType.DateTime,
        [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
        [typeof(TimeSpan)] = DbType.Time,
        [typeof(Guid)] = DbType.Guid,
        [typeof(JsonElement)] = DbType.String,
        [typeof(object)] = DbType.Object,
#if NET5_0_OR_GREATER
        [typeof(Half)] = DbType.Single,
#endif
#if NET6_0_OR_GREATER
        [typeof(DateOnly)] = DbType.Date,
        [typeof(TimeOnly)] = DbType.Time,
#endif
#if NET7_0_OR_GREATER
        [typeof(Int128)] = DbType.VarNumeric,
        [typeof(UInt128)] = DbType.VarNumeric,
#endif
    }.ToFrozenDictionary();

    /// <summary>
    /// Returns the <see cref="DbType"/> that best represents the specified CLR <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The CLR type to map.</param>
    /// <returns>
    /// The corresponding <see cref="DbType"/> value, or <see langword="null"/> if no mapping exists.
    /// </returns>
    public static DbType? ToDbType(this Type? type)
    {
        if (type is null)
            return null;

        // Unwrap Nullable<T> so that int? maps the same as int.
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return _systemToDbType.GetValueOrDefault(underlying);
    }

    /// <summary>
    /// Returns the CLR <see cref="Type"/> that the specified <paramref name="dbType"/> maps to.
    /// </summary>
    /// <param name="dbType">The <see cref="DbType"/> value to map.</param>
    /// <returns>
    /// The corresponding CLR type, or <see langword="null"/> if no mapping exists.
    /// </returns>
    public static Type? ToSystemType(this DbType dbType)
    {
        return _dbTypeToSystem.GetValueOrDefault(dbType);
    }
}
