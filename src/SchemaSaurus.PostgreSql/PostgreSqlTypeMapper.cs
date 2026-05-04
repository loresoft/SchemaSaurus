using System.Collections.Frozen;
using System.Data;

using NpgsqlTypes;

namespace SchemaSaurus.PostgreSql;

/// <summary>
/// Provides mappings from PostgreSQL native data type names to common .NET data type metadata.
/// </summary>
public static class PostgreSqlTypeMapper
{
    // Mapping of PostgreSQL system type names to DbType, NpgsqlDbType, CLR type, and Unicode/fixed-length attributes.
    private static readonly FrozenDictionary<string, (DbType DbType, NpgsqlDbType NpgsqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)> PostgreSqlTypeMappings
        = new Dictionary<string, (DbType DbType, NpgsqlDbType NpgsqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)>(StringComparer.OrdinalIgnoreCase)
        {
            ["bool"] = (DbType.Boolean, NpgsqlDbType.Boolean, typeof(bool), null, null),
            ["boolean"] = (DbType.Boolean, NpgsqlDbType.Boolean, typeof(bool), null, null),
            ["int2"] = (DbType.Int16, NpgsqlDbType.Smallint, typeof(short), null, null),
            ["smallint"] = (DbType.Int16, NpgsqlDbType.Smallint, typeof(short), null, null),
            ["int4"] = (DbType.Int32, NpgsqlDbType.Integer, typeof(int), null, null),
            ["integer"] = (DbType.Int32, NpgsqlDbType.Integer, typeof(int), null, null),
            ["int8"] = (DbType.Int64, NpgsqlDbType.Bigint, typeof(long), null, null),
            ["bigint"] = (DbType.Int64, NpgsqlDbType.Bigint, typeof(long), null, null),
            ["float4"] = (DbType.Single, NpgsqlDbType.Real, typeof(float), null, null),
            ["real"] = (DbType.Single, NpgsqlDbType.Real, typeof(float), null, null),
            ["float8"] = (DbType.Double, NpgsqlDbType.Double, typeof(double), null, null),
            ["double precision"] = (DbType.Double, NpgsqlDbType.Double, typeof(double), null, null),
            ["numeric"] = (DbType.Decimal, NpgsqlDbType.Numeric, typeof(decimal), null, null),
            ["decimal"] = (DbType.Decimal, NpgsqlDbType.Numeric, typeof(decimal), null, null),
            ["money"] = (DbType.Currency, NpgsqlDbType.Money, typeof(decimal), null, null),
            ["text"] = (DbType.String, NpgsqlDbType.Text, typeof(string), true, false),
            ["varchar"] = (DbType.String, NpgsqlDbType.Varchar, typeof(string), true, false),
            ["character varying"] = (DbType.String, NpgsqlDbType.Varchar, typeof(string), true, false),
            ["bpchar"] = (DbType.StringFixedLength, NpgsqlDbType.Char, typeof(string), true, true),
            ["char"] = (DbType.StringFixedLength, NpgsqlDbType.Char, typeof(string), true, true),
            ["character"] = (DbType.StringFixedLength, NpgsqlDbType.Char, typeof(string), true, true),
            ["bytea"] = (DbType.Binary, NpgsqlDbType.Bytea, typeof(byte[]), null, false),
            ["uuid"] = (DbType.Guid, NpgsqlDbType.Uuid, typeof(Guid), null, null),
            ["json"] = (DbType.String, NpgsqlDbType.Json, typeof(string), true, false),
            ["jsonb"] = (DbType.String, NpgsqlDbType.Jsonb, typeof(string), true, false),
            ["xml"] = (DbType.Xml, NpgsqlDbType.Xml, typeof(string), true, false),
            ["date"] = (DbType.Date, NpgsqlDbType.Date, typeof(DateOnly), null, null),
            ["time"] = (DbType.Time, NpgsqlDbType.Time, typeof(TimeOnly), null, null),
            ["time without time zone"] = (DbType.Time, NpgsqlDbType.Time, typeof(TimeOnly), null, null),
            ["timetz"] = (DbType.Time, NpgsqlDbType.TimeTz, typeof(DateTimeOffset), null, null),
            ["time with time zone"] = (DbType.Time, NpgsqlDbType.TimeTz, typeof(DateTimeOffset), null, null),
            ["timestamp"] = (DbType.DateTime2, NpgsqlDbType.Timestamp, typeof(DateTime), null, null),
            ["timestamp without time zone"] = (DbType.DateTime2, NpgsqlDbType.Timestamp, typeof(DateTime), null, null),
            ["timestamptz"] = (DbType.DateTimeOffset, NpgsqlDbType.TimestampTz, typeof(DateTimeOffset), null, null),
            ["timestamp with time zone"] = (DbType.DateTimeOffset, NpgsqlDbType.TimestampTz, typeof(DateTimeOffset), null, null),
            ["interval"] = (DbType.Object, NpgsqlDbType.Interval, typeof(TimeSpan), null, null),
            ["record"] = (DbType.Object, NpgsqlDbType.Unknown, typeof(object), null, null),
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<DbType, NpgsqlDbType> DbTypeToNpgsqlDbTypeMappings
        = new Dictionary<DbType, NpgsqlDbType>
        {
            [DbType.Binary] = NpgsqlDbType.Bytea,
            [DbType.Boolean] = NpgsqlDbType.Boolean,
            [DbType.Currency] = NpgsqlDbType.Money,
            [DbType.Date] = NpgsqlDbType.Date,
            [DbType.DateTime] = NpgsqlDbType.Timestamp,
            [DbType.DateTime2] = NpgsqlDbType.Timestamp,
            [DbType.DateTimeOffset] = NpgsqlDbType.TimestampTz,
            [DbType.Decimal] = NpgsqlDbType.Numeric,
            [DbType.Double] = NpgsqlDbType.Double,
            [DbType.Guid] = NpgsqlDbType.Uuid,
            [DbType.Int16] = NpgsqlDbType.Smallint,
            [DbType.Int32] = NpgsqlDbType.Integer,
            [DbType.Int64] = NpgsqlDbType.Bigint,
            [DbType.Object] = NpgsqlDbType.Unknown,
            [DbType.Single] = NpgsqlDbType.Real,
            [DbType.String] = NpgsqlDbType.Text,
            [DbType.StringFixedLength] = NpgsqlDbType.Char,
            [DbType.Time] = NpgsqlDbType.Time,
            [DbType.Xml] = NpgsqlDbType.Xml,
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<NpgsqlDbType, DbType> NpgsqlDbTypeToDbTypeMappings
        = new Dictionary<NpgsqlDbType, DbType>
        {
            [NpgsqlDbType.Bigint] = DbType.Int64,
            [NpgsqlDbType.Boolean] = DbType.Boolean,
            [NpgsqlDbType.Bytea] = DbType.Binary,
            [NpgsqlDbType.Char] = DbType.StringFixedLength,
            [NpgsqlDbType.Date] = DbType.Date,
            [NpgsqlDbType.Double] = DbType.Double,
            [NpgsqlDbType.Integer] = DbType.Int32,
            [NpgsqlDbType.Json] = DbType.String,
            [NpgsqlDbType.Jsonb] = DbType.String,
            [NpgsqlDbType.Money] = DbType.Currency,
            [NpgsqlDbType.Numeric] = DbType.Decimal,
            [NpgsqlDbType.Real] = DbType.Single,
            [NpgsqlDbType.Smallint] = DbType.Int16,
            [NpgsqlDbType.Text] = DbType.String,
            [NpgsqlDbType.Time] = DbType.Time,
            [NpgsqlDbType.Timestamp] = DbType.DateTime2,
            [NpgsqlDbType.TimestampTz] = DbType.DateTimeOffset,
            [NpgsqlDbType.TimeTz] = DbType.Time,
            [NpgsqlDbType.Unknown] = DbType.Object,
            [NpgsqlDbType.Uuid] = DbType.Guid,
            [NpgsqlDbType.Varchar] = DbType.String,
            [NpgsqlDbType.Xml] = DbType.Xml,
        }.ToFrozenDictionary();

    /// <summary>
    /// Maps a PostgreSQL native data type name to its corresponding <see cref="DbType" />, CLR type, and text attributes.
    /// </summary>
    /// <param name="typeName">The PostgreSQL native data type name to map.</param>
    /// <returns>
    /// A tuple containing the mapped <see cref="DbType" />, <see cref="NpgsqlDbType" />, CLR system type, Unicode flag, and fixed-length flag.
    /// Unknown type names map to <see cref="DbType.Object" />, <see cref="NpgsqlDbType.Unknown" />, <see cref="object" />, and unspecified text attributes.
    /// </returns>
    public static (DbType DbType, NpgsqlDbType NpgsqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapNativeType(string typeName)
    {
        if (PostgreSqlTypeMappings.TryGetValue(typeName, out var mapping))
            return mapping;

        return (DbType.Object, NpgsqlDbType.Unknown, typeof(object), null, null);
    }

    /// <summary>
    /// Maps a <see cref="DbType" /> value to its closest PostgreSQL-specific <see cref="NpgsqlDbType" /> value.
    /// </summary>
    /// <param name="dbType">The provider-independent database type to map.</param>
    /// <returns>The closest matching PostgreSQL-specific type, or <see cref="NpgsqlDbType.Unknown" /> when no mapping exists.</returns>
    public static NpgsqlDbType ToNpgsqlDbType(DbType dbType)
    {
        if (DbTypeToNpgsqlDbTypeMappings.TryGetValue(dbType, out var npgsqlDbType))
            return npgsqlDbType;

        return NpgsqlDbType.Unknown;
    }

    /// <summary>
    /// Maps a PostgreSQL-specific <see cref="NpgsqlDbType" /> value to its closest provider-independent <see cref="DbType" /> value.
    /// </summary>
    /// <param name="npgsqlDbType">The PostgreSQL-specific database type to map.</param>
    /// <returns>The closest matching provider-independent type, or <see cref="DbType.Object" /> when no mapping exists.</returns>
    public static DbType ToDbType(NpgsqlDbType npgsqlDbType)
    {
        if (NpgsqlDbTypeToDbTypeMappings.TryGetValue(npgsqlDbType, out var dbType))
            return dbType;

        return DbType.Object;
    }
}
