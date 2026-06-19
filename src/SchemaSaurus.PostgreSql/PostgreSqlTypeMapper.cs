using System.Collections.Frozen;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net;

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
            ["cid"] = (DbType.UInt32, NpgsqlDbType.Cid, typeof(uint), null, null),
            ["oid"] = (DbType.UInt32, NpgsqlDbType.Oid, typeof(uint), null, null),
            ["xid"] = (DbType.UInt32, NpgsqlDbType.Xid, typeof(uint), null, null),
            ["int8"] = (DbType.Int64, NpgsqlDbType.Bigint, typeof(long), null, null),
            ["bigint"] = (DbType.Int64, NpgsqlDbType.Bigint, typeof(long), null, null),
            ["xid8"] = (DbType.UInt64, NpgsqlDbType.Xid8, typeof(ulong), null, null),
            ["float4"] = (DbType.Single, NpgsqlDbType.Real, typeof(float), null, null),
            ["real"] = (DbType.Single, NpgsqlDbType.Real, typeof(float), null, null),
            ["float8"] = (DbType.Double, NpgsqlDbType.Double, typeof(double), null, null),
            ["double precision"] = (DbType.Double, NpgsqlDbType.Double, typeof(double), null, null),
            ["numeric"] = (DbType.Decimal, NpgsqlDbType.Numeric, typeof(decimal), null, null),
            ["decimal"] = (DbType.Decimal, NpgsqlDbType.Numeric, typeof(decimal), null, null),
            ["money"] = (DbType.Currency, NpgsqlDbType.Money, typeof(decimal), null, null),
            ["text"] = (DbType.String, NpgsqlDbType.Text, typeof(string), true, false),
            ["citext"] = (DbType.String, NpgsqlDbType.Citext, typeof(string), true, false),
            ["jsonpath"] = (DbType.String, NpgsqlDbType.JsonPath, typeof(string), true, false),
            ["refcursor"] = (DbType.String, NpgsqlDbType.Refcursor, typeof(string), true, false),
            ["tsquery"] = (DbType.String, NpgsqlDbType.TsQuery, typeof(NpgsqlTsQuery), true, false),
            ["tsvector"] = (DbType.String, NpgsqlDbType.TsVector, typeof(NpgsqlTsVector), true, false),
            ["varchar"] = (DbType.String, NpgsqlDbType.Varchar, typeof(string), true, false),
            ["character varying"] = (DbType.String, NpgsqlDbType.Varchar, typeof(string), true, false),
            ["name"] = (DbType.StringFixedLength, NpgsqlDbType.Name, typeof(string), true, true),
            ["bpchar"] = (DbType.StringFixedLength, NpgsqlDbType.Char, typeof(string), true, true),
            ["char"] = (DbType.StringFixedLength, NpgsqlDbType.Char, typeof(string), true, true),
            ["character"] = (DbType.StringFixedLength, NpgsqlDbType.Char, typeof(string), true, true),
            ["bit"] = (DbType.StringFixedLength, NpgsqlDbType.Bit, typeof(string), null, true),
            ["bit varying"] = (DbType.String, NpgsqlDbType.Varbit, typeof(string), null, false),
            ["varbit"] = (DbType.String, NpgsqlDbType.Varbit, typeof(string), null, false),
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
            ["cidr"] = (DbType.Object, NpgsqlDbType.Cidr, typeof(IPNetwork), null, null),
            ["inet"] = (DbType.Object, NpgsqlDbType.Inet, typeof(NpgsqlInet), null, null),
            ["macaddr"] = (DbType.Object, NpgsqlDbType.MacAddr, typeof(System.Net.NetworkInformation.PhysicalAddress), null, null),
            ["macaddr8"] = (DbType.Object, NpgsqlDbType.MacAddr8, typeof(System.Net.NetworkInformation.PhysicalAddress), null, null),
            ["pg_lsn"] = (DbType.Object, NpgsqlDbType.PgLsn, typeof(NpgsqlLogSequenceNumber), null, null),
            ["geometry"] = (DbType.Object, NpgsqlDbType.Geometry, typeof(object), null, null),
            ["geography"] = (DbType.Object, NpgsqlDbType.Geography, typeof(object), null, null),
            ["record"] = (DbType.Object, NpgsqlDbType.Unknown, typeof(object), null, null),
            ["tid"] = (DbType.Object, NpgsqlDbType.Tid, typeof(NpgsqlTid), null, null),
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
            [DbType.UInt32] = NpgsqlDbType.Xid,
            [DbType.UInt64] = NpgsqlDbType.Xid8,
            [DbType.Xml] = NpgsqlDbType.Xml,
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<NpgsqlDbType, DbType> NpgsqlDbTypeToDbTypeMappings
        = new Dictionary<NpgsqlDbType, DbType>
        {
            [NpgsqlDbType.Bigint] = DbType.Int64,
            [NpgsqlDbType.Bit] = DbType.StringFixedLength,
            [NpgsqlDbType.Boolean] = DbType.Boolean,
            [NpgsqlDbType.Bytea] = DbType.Binary,
            [NpgsqlDbType.Char] = DbType.StringFixedLength,
            [NpgsqlDbType.Cid] = DbType.UInt32,
            [NpgsqlDbType.Date] = DbType.Date,
            [NpgsqlDbType.Double] = DbType.Double,
            [NpgsqlDbType.Integer] = DbType.Int32,
            [NpgsqlDbType.Json] = DbType.String,
            [NpgsqlDbType.Jsonb] = DbType.String,
            [NpgsqlDbType.JsonPath] = DbType.String,
            [NpgsqlDbType.Geometry] = DbType.Object,
            [NpgsqlDbType.Geography] = DbType.Object,
            [NpgsqlDbType.Money] = DbType.Currency,
            [NpgsqlDbType.Name] = DbType.StringFixedLength,
            [NpgsqlDbType.Numeric] = DbType.Decimal,
            [NpgsqlDbType.Oid] = DbType.UInt32,
            [NpgsqlDbType.Real] = DbType.Single,
            [NpgsqlDbType.Refcursor] = DbType.String,
            [NpgsqlDbType.Smallint] = DbType.Int16,
            [NpgsqlDbType.Text] = DbType.String,
            [NpgsqlDbType.Time] = DbType.Time,
            [NpgsqlDbType.Timestamp] = DbType.DateTime2,
            [NpgsqlDbType.TimestampTz] = DbType.DateTimeOffset,
            [NpgsqlDbType.TimeTz] = DbType.Time,
            [NpgsqlDbType.Unknown] = DbType.Object,
            [NpgsqlDbType.Uuid] = DbType.Guid,
            [NpgsqlDbType.Varbit] = DbType.String,
            [NpgsqlDbType.Varchar] = DbType.String,
            [NpgsqlDbType.Xid] = DbType.UInt32,
            [NpgsqlDbType.Xid8] = DbType.UInt64,
            [NpgsqlDbType.Xml] = DbType.Xml,
        }.ToFrozenDictionary();

    /// <summary>
    /// Maps a PostgreSQL native data type name to its corresponding <see cref="DbType"/>, provider type, CLR type, and text attributes.
    /// </summary>
    /// <param name="typeName">The PostgreSQL native type name (for example, <c>varchar</c> or <c>timestamp with time zone</c>).</param>
    /// <returns>
    /// A tuple containing mapped <see cref="DbType"/>, <see cref="NpgsqlDbType"/>, CLR <see cref="Type"/>,
    /// and optional Unicode/fixed-length flags. Unknown types map to object/unknown defaults.
    /// </returns>
    public static (DbType DbType, NpgsqlDbType NpgsqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapNativeType(string typeName)
    {
        if (TryGetArrayElementTypeName(typeName, out var elementTypeName))
            return MapArrayNativeType(elementTypeName);

        if (PostgreSqlTypeMappings.TryGetValue(typeName, out var mapping))
            return mapping;

        return (DbType.Object, NpgsqlDbType.Unknown, typeof(object), null, null);
    }

    /// <summary>
    /// Maps a PostgreSQL array element native data type name to array metadata.
    /// </summary>
    /// <param name="elementTypeName">The PostgreSQL native element type name (for example, <c>int4</c> or <c>text</c>).</param>
    /// <returns>
    /// A tuple containing mapped array <see cref="DbType"/>, array <see cref="NpgsqlDbType"/>, CLR array <see cref="Type"/>,
    /// and optional Unicode/fixed-length flags inherited from the element type.
    /// </returns>
    public static (DbType DbType, NpgsqlDbType NpgsqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapArrayNativeType(string elementTypeName)
    {
        if (TryGetArrayElementTypeName(elementTypeName, out var nestedElementTypeName))
            elementTypeName = nestedElementTypeName;

        var elementMapping = MapNativeType(elementTypeName);
        var npgsqlDbType = NpgsqlDbType.Array | elementMapping.NpgsqlDbType;
        var systemType = elementMapping.SystemType.MakeArrayType();

        return (DbType.Object, npgsqlDbType, systemType, elementMapping.IsUnicode, elementMapping.IsFixedLength);
    }

    private static bool TryGetArrayElementTypeName(string typeName, [NotNullWhen(true)] out string? elementTypeName)
    {
        const string suffix = "[]";

        if (typeName.EndsWith(suffix, StringComparison.Ordinal))
        {
            elementTypeName = typeName[..^suffix.Length];
            return elementTypeName.Length > 0;
        }

        elementTypeName = null;
        return false;
    }

    /// <summary>
    /// Maps a <see cref="DbType"/> value to its closest PostgreSQL-specific <see cref="NpgsqlDbType"/> value.
    /// </summary>
    /// <param name="dbType">The provider-independent database type to map.</param>
    /// <returns>The closest matching <see cref="NpgsqlDbType"/> value. Unknown values map to <see cref="NpgsqlDbType.Unknown"/>.</returns>
    public static NpgsqlDbType ToNpgsqlDbType(DbType dbType)
    {
        if (DbTypeToNpgsqlDbTypeMappings.TryGetValue(dbType, out var npgsqlDbType))
            return npgsqlDbType;

        return NpgsqlDbType.Unknown;
    }

    /// <summary>
    /// Maps a PostgreSQL-specific <see cref="NpgsqlDbType"/> value to its closest provider-independent <see cref="DbType"/> value.
    /// </summary>
    /// <param name="npgsqlDbType">The PostgreSQL-specific database type to map.</param>
    /// <returns>The closest matching <see cref="DbType"/> value. Unknown values map to <see cref="DbType.Object"/>.</returns>
    public static DbType ToDbType(NpgsqlDbType npgsqlDbType)
    {
        if (NpgsqlDbTypeToDbTypeMappings.TryGetValue(npgsqlDbType, out var dbType))
            return dbType;

        return DbType.Object;
    }
}
