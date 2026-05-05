using System.Collections.Frozen;
using System.Data;

using MySqlConnector;

namespace SchemaSaurus.MySql;

/// <summary>
/// Provides mappings from MySQL native data type names to common .NET data type metadata.
/// </summary>
public static class MySqlTypeMapper
{
    private static readonly FrozenDictionary<string, (DbType DbType, MySqlDbType MySqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)> MySqlTypeMappings
        = new Dictionary<string, (DbType DbType, MySqlDbType MySqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)>(StringComparer.OrdinalIgnoreCase)
        {
            ["tinyint"] = (DbType.SByte, MySqlDbType.Byte, typeof(sbyte), null, null),
            ["smallint"] = (DbType.Int16, MySqlDbType.Int16, typeof(short), null, null),
            ["mediumint"] = (DbType.Int32, MySqlDbType.Int24, typeof(int), null, null),
            ["int"] = (DbType.Int32, MySqlDbType.Int32, typeof(int), null, null),
            ["integer"] = (DbType.Int32, MySqlDbType.Int32, typeof(int), null, null),
            ["bigint"] = (DbType.Int64, MySqlDbType.Int64, typeof(long), null, null),
            ["bit"] = (DbType.UInt64, MySqlDbType.Bit, typeof(ulong), null, null),
            ["bool"] = (DbType.Boolean, MySqlDbType.Bool, typeof(bool), null, null),
            ["boolean"] = (DbType.Boolean, MySqlDbType.Bool, typeof(bool), null, null),
            ["decimal"] = (DbType.Decimal, MySqlDbType.Decimal, typeof(decimal), null, null),
            ["dec"] = (DbType.Decimal, MySqlDbType.Decimal, typeof(decimal), null, null),
            ["numeric"] = (DbType.Decimal, MySqlDbType.Decimal, typeof(decimal), null, null),
            ["fixed"] = (DbType.Decimal, MySqlDbType.Decimal, typeof(decimal), null, null),
            ["float"] = (DbType.Single, MySqlDbType.Float, typeof(float), null, null),
            ["double"] = (DbType.Double, MySqlDbType.Double, typeof(double), null, null),
            ["real"] = (DbType.Double, MySqlDbType.Double, typeof(double), null, null),
            ["date"] = (DbType.Date, MySqlDbType.Date, GetDateType(), null, null),
            ["time"] = (DbType.Time, MySqlDbType.Time, GetTimeType(), null, null),
            ["datetime"] = (DbType.DateTime2, MySqlDbType.DateTime, typeof(DateTime), null, null),
            ["timestamp"] = (DbType.DateTime, MySqlDbType.Timestamp, typeof(DateTime), null, null),
            ["year"] = (DbType.Int16, MySqlDbType.Year, typeof(short), null, null),
            ["char"] = (DbType.StringFixedLength, MySqlDbType.String, typeof(string), true, true),
            ["varchar"] = (DbType.String, MySqlDbType.VarChar, typeof(string), true, false),
            ["tinytext"] = (DbType.String, MySqlDbType.TinyText, typeof(string), true, false),
            ["text"] = (DbType.String, MySqlDbType.Text, typeof(string), true, false),
            ["mediumtext"] = (DbType.String, MySqlDbType.MediumText, typeof(string), true, false),
            ["longtext"] = (DbType.String, MySqlDbType.LongText, typeof(string), true, false),
            ["json"] = (DbType.String, MySqlDbType.JSON, typeof(string), true, false),
            ["enum"] = (DbType.String, MySqlDbType.Enum, typeof(string), true, false),
            ["set"] = (DbType.String, MySqlDbType.Set, typeof(string), true, false),
            ["binary"] = (DbType.Binary, MySqlDbType.Binary, typeof(byte[]), null, true),
            ["varbinary"] = (DbType.Binary, MySqlDbType.VarBinary, typeof(byte[]), null, false),
            ["tinyblob"] = (DbType.Binary, MySqlDbType.TinyBlob, typeof(byte[]), null, false),
            ["blob"] = (DbType.Binary, MySqlDbType.Blob, typeof(byte[]), null, false),
            ["mediumblob"] = (DbType.Binary, MySqlDbType.MediumBlob, typeof(byte[]), null, false),
            ["longblob"] = (DbType.Binary, MySqlDbType.LongBlob, typeof(byte[]), null, false),
            ["geometry"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
            ["point"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
            ["linestring"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
            ["polygon"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
            ["multipoint"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
            ["multilinestring"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
            ["multipolygon"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
            ["geometrycollection"] = (DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null),
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<DbType, MySqlDbType> DbTypeToMySqlDbTypeMappings
        = new Dictionary<DbType, MySqlDbType>
        {
            [DbType.AnsiString] = MySqlDbType.VarChar,
            [DbType.AnsiStringFixedLength] = MySqlDbType.String,
            [DbType.Binary] = MySqlDbType.Blob,
            [DbType.Boolean] = MySqlDbType.Bool,
            [DbType.Byte] = MySqlDbType.UByte,
            [DbType.Date] = MySqlDbType.Date,
            [DbType.DateTime] = MySqlDbType.DateTime,
            [DbType.DateTime2] = MySqlDbType.DateTime,
            [DbType.Decimal] = MySqlDbType.Decimal,
            [DbType.Double] = MySqlDbType.Double,
            [DbType.Guid] = MySqlDbType.Guid,
            [DbType.Int16] = MySqlDbType.Int16,
            [DbType.Int32] = MySqlDbType.Int32,
            [DbType.Int64] = MySqlDbType.Int64,
            [DbType.Object] = MySqlDbType.JSON,
            [DbType.SByte] = MySqlDbType.Byte,
            [DbType.Single] = MySqlDbType.Float,
            [DbType.String] = MySqlDbType.VarChar,
            [DbType.StringFixedLength] = MySqlDbType.String,
            [DbType.Time] = MySqlDbType.Time,
            [DbType.UInt16] = MySqlDbType.UInt16,
            [DbType.UInt32] = MySqlDbType.UInt32,
            [DbType.UInt64] = MySqlDbType.UInt64,
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<MySqlDbType, DbType> MySqlDbTypeToDbTypeMappings
        = new Dictionary<MySqlDbType, DbType>
        {
            [MySqlDbType.Binary] = DbType.Binary,
            [MySqlDbType.Bit] = DbType.UInt64,
            [MySqlDbType.Blob] = DbType.Binary,
            [MySqlDbType.Bool] = DbType.Boolean,
            [MySqlDbType.Byte] = DbType.SByte,
            [MySqlDbType.Date] = DbType.Date,
            [MySqlDbType.DateTime] = DbType.DateTime2,
            [MySqlDbType.Decimal] = DbType.Decimal,
            [MySqlDbType.Double] = DbType.Double,
            [MySqlDbType.Enum] = DbType.String,
            [MySqlDbType.Float] = DbType.Single,
            [MySqlDbType.Guid] = DbType.Guid,
            [MySqlDbType.Int16] = DbType.Int16,
            [MySqlDbType.Int24] = DbType.Int32,
            [MySqlDbType.Int32] = DbType.Int32,
            [MySqlDbType.Int64] = DbType.Int64,
            [MySqlDbType.JSON] = DbType.String,
            [MySqlDbType.LongBlob] = DbType.Binary,
            [MySqlDbType.LongText] = DbType.String,
            [MySqlDbType.MediumBlob] = DbType.Binary,
            [MySqlDbType.MediumText] = DbType.String,
            [MySqlDbType.Set] = DbType.String,
            [MySqlDbType.String] = DbType.StringFixedLength,
            [MySqlDbType.Text] = DbType.String,
            [MySqlDbType.Time] = DbType.Time,
            [MySqlDbType.Timestamp] = DbType.DateTime,
            [MySqlDbType.TinyBlob] = DbType.Binary,
            [MySqlDbType.TinyText] = DbType.String,
            [MySqlDbType.UByte] = DbType.Byte,
            [MySqlDbType.UInt16] = DbType.UInt16,
            [MySqlDbType.UInt24] = DbType.UInt32,
            [MySqlDbType.UInt32] = DbType.UInt32,
            [MySqlDbType.UInt64] = DbType.UInt64,
            [MySqlDbType.VarBinary] = DbType.Binary,
            [MySqlDbType.VarChar] = DbType.String,
            [MySqlDbType.Year] = DbType.Int16,
        }.ToFrozenDictionary();

    /// <summary>
    /// Maps a MySQL native data type name to its corresponding <see cref="DbType" />, provider type, CLR type, and text attributes.
    /// </summary>
    public static (DbType DbType, MySqlDbType MySqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapNativeType(string typeName)
    {
        if (MySqlTypeMappings.TryGetValue(typeName, out var mapping))
            return mapping;

        return (DbType.Object, MySqlDbType.JSON, typeof(object), null, null);
    }

    /// <summary>
    /// Maps a <see cref="DbType" /> value to its closest MySQL-specific <see cref="MySqlDbType" /> value.
    /// </summary>
    public static MySqlDbType ToMySqlDbType(DbType dbType)
    {
        if (DbTypeToMySqlDbTypeMappings.TryGetValue(dbType, out var mySqlDbType))
            return mySqlDbType;

        return MySqlDbType.JSON;
    }

    /// <summary>
    /// Maps a MySQL-specific <see cref="MySqlDbType" /> value to its closest provider-independent <see cref="DbType" /> value.
    /// </summary>
    public static DbType ToDbType(MySqlDbType mySqlDbType)
    {
        if (MySqlDbTypeToDbTypeMappings.TryGetValue(mySqlDbType, out var dbType))
            return dbType;

        return DbType.Object;
    }

    private static Type GetDateType()
    {
#if NET6_0_OR_GREATER
        return typeof(DateOnly);
#else
        return typeof(DateTime);
#endif
    }

    private static Type GetTimeType()
    {
#if NET6_0_OR_GREATER
        return typeof(TimeOnly);
#else
        return typeof(TimeSpan);
#endif
    }
}
