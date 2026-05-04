using System.Collections.Frozen;
using System.Data;

namespace SchemaSaurus.SqlServer;

/// <summary>
/// Provides mappings from SQL Server native data type names to common .NET data type metadata.
/// </summary>
public static class SqlServerTypeMapper
{
    // Mapping of SQL Server system type names to DbType, SqlDbType, CLR type, and Unicode/fixed-length attributes.
    private static readonly FrozenDictionary<string, (DbType DbType, SqlDbType SqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)> SqlServerTypeMappings
        = new Dictionary<string, (DbType DbType, SqlDbType SqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)>(StringComparer.OrdinalIgnoreCase)
        {
            ["bigint"] = (DbType.Int64, SqlDbType.BigInt, typeof(long), null, null),
            ["int"] = (DbType.Int32, SqlDbType.Int, typeof(int), null, null),
            ["smallint"] = (DbType.Int16, SqlDbType.SmallInt, typeof(short), null, null),
            ["tinyint"] = (DbType.Byte, SqlDbType.TinyInt, typeof(byte), null, null),
            ["bit"] = (DbType.Boolean, SqlDbType.Bit, typeof(bool), null, null),
            ["decimal"] = (DbType.Decimal, SqlDbType.Decimal, typeof(decimal), null, null),
            ["numeric"] = (DbType.Decimal, SqlDbType.Decimal, typeof(decimal), null, null),
            ["money"] = (DbType.Currency, SqlDbType.Money, typeof(decimal), null, null),
            ["smallmoney"] = (DbType.Currency, SqlDbType.SmallMoney, typeof(decimal), null, null),
            ["float"] = (DbType.Double, SqlDbType.Float, typeof(double), null, null),
            ["real"] = (DbType.Single, SqlDbType.Real, typeof(float), null, null),
            ["datetime"] = (DbType.DateTime, SqlDbType.DateTime, typeof(DateTime), null, null),
            ["smalldatetime"] = (DbType.DateTime, SqlDbType.SmallDateTime, typeof(DateTime), null, null),
            ["datetime2"] = (DbType.DateTime2, SqlDbType.DateTime2, typeof(DateTime), null, null),
            ["datetimeoffset"] = (DbType.DateTimeOffset, SqlDbType.DateTimeOffset, typeof(DateTimeOffset), null, null),
            ["char"] = (DbType.AnsiStringFixedLength, SqlDbType.Char, typeof(string), false, true),
            ["varchar"] = (DbType.AnsiString, SqlDbType.VarChar, typeof(string), false, false),
            ["text"] = (DbType.AnsiString, SqlDbType.Text, typeof(string), false, false),
            ["nchar"] = (DbType.StringFixedLength, SqlDbType.NChar, typeof(string), true, true),
            ["nvarchar"] = (DbType.String, SqlDbType.NVarChar, typeof(string), true, false),
            ["ntext"] = (DbType.String, SqlDbType.NText, typeof(string), true, false),
            ["json"] = (DbType.String, GetJsonSqlDbType(), typeof(string), true, false),
            ["binary"] = (DbType.Binary, SqlDbType.Binary, typeof(byte[]), null, true),
            ["varbinary"] = (DbType.Binary, SqlDbType.VarBinary, typeof(byte[]), null, false),
            ["image"] = (DbType.Binary, SqlDbType.Image, typeof(byte[]), null, false),
            ["timestamp"] = (DbType.Binary, SqlDbType.Timestamp, typeof(byte[]), null, null),
            ["rowversion"] = (DbType.Binary, SqlDbType.Timestamp, typeof(byte[]), null, null),
            ["uniqueidentifier"] = (DbType.Guid, SqlDbType.UniqueIdentifier, typeof(Guid), null, null),
            ["xml"] = (DbType.Xml, SqlDbType.Xml, typeof(string), null, null),
            ["vector"] = (DbType.Object, GetVectorSqlDbType(), typeof(float[]), null, null),
            ["sql_variant"] = (DbType.Object, SqlDbType.Variant, typeof(object), null, null),
#if NET6_0_OR_GREATER
            ["time"] = (DbType.Time, SqlDbType.Time, typeof(TimeOnly), null, null),
            ["date"] = (DbType.Date, SqlDbType.Date, typeof(DateOnly), null, null),
#else
            ["time"] = (DbType.Time, SqlDbType.Time, typeof(TimeSpan), null, null),
            ["date"] = (DbType.Date, SqlDbType.Date, typeof(DateTime), null, null),
#endif
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<DbType, SqlDbType> DbTypeToSqlDbTypeMappings
        = new Dictionary<DbType, SqlDbType>
        {
            [DbType.AnsiString] = SqlDbType.VarChar,
            [DbType.AnsiStringFixedLength] = SqlDbType.Char,
            [DbType.Binary] = SqlDbType.VarBinary,
            [DbType.Boolean] = SqlDbType.Bit,
            [DbType.Byte] = SqlDbType.TinyInt,
            [DbType.Currency] = SqlDbType.Money,
            [DbType.Date] = SqlDbType.Date,
            [DbType.DateTime] = SqlDbType.DateTime,
            [DbType.DateTime2] = SqlDbType.DateTime2,
            [DbType.DateTimeOffset] = SqlDbType.DateTimeOffset,
            [DbType.Decimal] = SqlDbType.Decimal,
            [DbType.Double] = SqlDbType.Float,
            [DbType.Guid] = SqlDbType.UniqueIdentifier,
            [DbType.Int16] = SqlDbType.SmallInt,
            [DbType.Int32] = SqlDbType.Int,
            [DbType.Int64] = SqlDbType.BigInt,
            [DbType.Object] = SqlDbType.Variant,
            [DbType.Single] = SqlDbType.Real,
            [DbType.String] = SqlDbType.NVarChar,
            [DbType.StringFixedLength] = SqlDbType.NChar,
            [DbType.Time] = SqlDbType.Time,
            [DbType.Xml] = SqlDbType.Xml,
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<SqlDbType, DbType> SqlDbTypeToDbTypeMappings
        = new Dictionary<SqlDbType, DbType>
        {
            [SqlDbType.BigInt] = DbType.Int64,
            [SqlDbType.Binary] = DbType.Binary,
            [SqlDbType.Bit] = DbType.Boolean,
            [SqlDbType.Char] = DbType.AnsiStringFixedLength,
            [SqlDbType.Date] = DbType.Date,
            [SqlDbType.DateTime] = DbType.DateTime,
            [SqlDbType.DateTime2] = DbType.DateTime2,
            [SqlDbType.DateTimeOffset] = DbType.DateTimeOffset,
            [SqlDbType.Decimal] = DbType.Decimal,
            [SqlDbType.Float] = DbType.Double,
            [SqlDbType.Image] = DbType.Binary,
            [SqlDbType.Int] = DbType.Int32,
            [SqlDbType.Money] = DbType.Currency,
            [SqlDbType.NChar] = DbType.StringFixedLength,
            [SqlDbType.NText] = DbType.String,
            [SqlDbType.NVarChar] = DbType.String,
            [SqlDbType.Real] = DbType.Single,
            [SqlDbType.SmallDateTime] = DbType.DateTime,
            [SqlDbType.SmallInt] = DbType.Int16,
            [SqlDbType.SmallMoney] = DbType.Currency,
            [SqlDbType.Structured] = DbType.Object,
            [SqlDbType.Text] = DbType.AnsiString,
            [SqlDbType.Time] = DbType.Time,
            [SqlDbType.Timestamp] = DbType.Binary,
            [SqlDbType.TinyInt] = DbType.Byte,
            [SqlDbType.Udt] = DbType.Object,
            [SqlDbType.UniqueIdentifier] = DbType.Guid,
            [SqlDbType.VarBinary] = DbType.Binary,
            [SqlDbType.VarChar] = DbType.AnsiString,
            [SqlDbType.Variant] = DbType.Object,
            [SqlDbType.Xml] = DbType.Xml,
#if NET9_0_OR_GREATER
            [SqlDbType.Json] = DbType.String,
#endif
#if NET10_0_OR_GREATER
            [SqlDbType.Vector] = DbType.Object,
#endif
        }.ToFrozenDictionary();


    /// <summary>
    /// Maps a SQL Server native data type name to its corresponding <see cref="DbType" />, CLR type, and text attributes.
    /// </summary>
    /// <param name="typeName">The SQL Server native data type name to map.</param>
    /// <returns>
    /// A tuple containing the mapped <see cref="DbType" />, CLR system type, Unicode flag, and fixed-length flag.
    /// Unknown type names map to <see cref="DbType.Object" />, <see cref="object" />, and unspecified text attributes.
    /// </returns>
    public static (DbType DbType, SqlDbType SqlDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapNativeType(string typeName)
    {
        if (SqlServerTypeMappings.TryGetValue(typeName, out var mapping))
            return mapping;

        return (DbType.Object, SqlDbType.Variant, typeof(object), null, null);
    }

    /// <summary>
    /// Maps a <see cref="DbType" /> value to its closest SQL Server-specific <see cref="SqlDbType" /> value.
    /// </summary>
    /// <param name="dbType">The provider-independent database type to map.</param>
    /// <returns>The closest matching SQL Server-specific type, or <see cref="SqlDbType.Variant" /> when no mapping exists.</returns>
    public static SqlDbType ToSqlDbType(DbType dbType)
    {
        if (DbTypeToSqlDbTypeMappings.TryGetValue(dbType, out var sqlDbType))
            return sqlDbType;

        return SqlDbType.Variant;
    }

    /// <summary>
    /// Maps a SQL Server-specific <see cref="SqlDbType" /> value to its closest provider-independent <see cref="DbType" /> value.
    /// </summary>
    /// <param name="sqlDbType">The SQL Server-specific database type to map.</param>
    /// <returns>The closest matching provider-independent type, or <see cref="DbType.Object" /> when no mapping exists.</returns>
    public static DbType ToDbType(SqlDbType sqlDbType)
    {
        if (SqlDbTypeToDbTypeMappings.TryGetValue(sqlDbType, out var dbType))
            return dbType;

        return DbType.Object;
    }


    private static SqlDbType GetJsonSqlDbType()
    {
#if NET9_0_OR_GREATER
        return SqlDbType.Json;
#else
        return SqlDbType.NVarChar;
#endif
    }

    private static SqlDbType GetVectorSqlDbType()
    {
#if NET10_0_OR_GREATER
        return SqlDbType.Vector;
#else
        return SqlDbType.Variant;
#endif
    }
}
