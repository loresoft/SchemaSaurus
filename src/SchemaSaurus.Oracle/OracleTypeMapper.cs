using System.Collections.Frozen;
using System.Data;

using Oracle.ManagedDataAccess.Client;

namespace SchemaSaurus.Oracle;

/// <summary>
/// Provides mappings from Oracle native data type names to common .NET data type metadata.
/// </summary>
public static class OracleTypeMapper
{
    private static readonly FrozenDictionary<string, (DbType DbType, OracleDbType OracleDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)> OracleTypeMappings
        = new Dictionary<string, (DbType DbType, OracleDbType OracleDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength)>(StringComparer.OrdinalIgnoreCase)
        {
            ["NUMBER"] = (DbType.Decimal, OracleDbType.Decimal, typeof(decimal), null, null),
            ["FLOAT"] = (DbType.Double, OracleDbType.Double, typeof(double), null, null),
            ["BINARY_FLOAT"] = (DbType.Single, OracleDbType.Single, typeof(float), null, null),
            ["BINARY_DOUBLE"] = (DbType.Double, OracleDbType.Double, typeof(double), null, null),
            ["CHAR"] = (DbType.AnsiStringFixedLength, OracleDbType.Char, typeof(string), false, true),
            ["VARCHAR2"] = (DbType.AnsiString, OracleDbType.Varchar2, typeof(string), false, false),
            ["VARCHAR"] = (DbType.AnsiString, OracleDbType.Varchar2, typeof(string), false, false),
            ["LONG"] = (DbType.AnsiString, OracleDbType.Long, typeof(string), false, false),
            ["NCHAR"] = (DbType.StringFixedLength, OracleDbType.NChar, typeof(string), true, true),
            ["NVARCHAR2"] = (DbType.String, OracleDbType.NVarchar2, typeof(string), true, false),
            ["CLOB"] = (DbType.AnsiString, OracleDbType.Clob, typeof(string), false, false),
            ["NCLOB"] = (DbType.String, OracleDbType.NClob, typeof(string), true, false),
            ["RAW"] = (DbType.Binary, OracleDbType.Raw, typeof(byte[]), null, false),
            ["LONG RAW"] = (DbType.Binary, OracleDbType.LongRaw, typeof(byte[]), null, false),
            ["BLOB"] = (DbType.Binary, OracleDbType.Blob, typeof(byte[]), null, false),
            ["DATE"] = (DbType.DateTime, OracleDbType.Date, typeof(DateTime), null, null),
            ["TIMESTAMP"] = (DbType.DateTime2, OracleDbType.TimeStamp, typeof(DateTime), null, null),
            ["TIMESTAMP WITH TIME ZONE"] = (DbType.DateTimeOffset, OracleDbType.TimeStampTZ, typeof(DateTimeOffset), null, null),
            ["TIMESTAMP WITH LOCAL TIME ZONE"] = (DbType.DateTimeOffset, OracleDbType.TimeStampLTZ, typeof(DateTimeOffset), null, null),
            ["INTERVAL YEAR TO MONTH"] = (DbType.Object, OracleDbType.IntervalYM, typeof(string), null, null),
            ["INTERVAL DAY TO SECOND"] = (DbType.Object, OracleDbType.IntervalDS, typeof(TimeSpan), null, null),
            ["ROWID"] = (DbType.AnsiString, OracleDbType.Varchar2, typeof(string), false, false),
            ["UROWID"] = (DbType.AnsiString, OracleDbType.Varchar2, typeof(string), false, false),
            ["XMLTYPE"] = (DbType.Xml, OracleDbType.XmlType, typeof(string), true, false),
            ["JSON"] = (DbType.String, OracleDbType.Json, typeof(string), true, false),
            ["BFILE"] = (DbType.Object, OracleDbType.BFile, typeof(byte[]), null, false),
            ["SDO_GEOMETRY"] = (DbType.Object, OracleDbType.Object, typeof(object), null, null),
            ["MDSYS.SDO_GEOMETRY"] = (DbType.Object, OracleDbType.Object, typeof(object), null, null),
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<DbType, OracleDbType> DbTypeToOracleDbTypeMappings
        = new Dictionary<DbType, OracleDbType>
        {
            [DbType.AnsiString] = OracleDbType.Varchar2,
            [DbType.AnsiStringFixedLength] = OracleDbType.Char,
            [DbType.Binary] = OracleDbType.Raw,
            [DbType.Boolean] = OracleDbType.Boolean,
            [DbType.Byte] = OracleDbType.Byte,
            [DbType.Currency] = OracleDbType.Decimal,
            [DbType.Date] = OracleDbType.Date,
            [DbType.DateTime] = OracleDbType.Date,
            [DbType.DateTime2] = OracleDbType.TimeStamp,
            [DbType.DateTimeOffset] = OracleDbType.TimeStampTZ,
            [DbType.Decimal] = OracleDbType.Decimal,
            [DbType.Double] = OracleDbType.Double,
            [DbType.Guid] = OracleDbType.Raw,
            [DbType.Int16] = OracleDbType.Int16,
            [DbType.Int32] = OracleDbType.Int32,
            [DbType.Int64] = OracleDbType.Int64,
            [DbType.Object] = OracleDbType.Object,
            [DbType.Single] = OracleDbType.Single,
            [DbType.String] = OracleDbType.NVarchar2,
            [DbType.StringFixedLength] = OracleDbType.NChar,
            [DbType.Time] = OracleDbType.IntervalDS,
            [DbType.Xml] = OracleDbType.XmlType,
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<OracleDbType, DbType> OracleDbTypeToDbTypeMappings
        = new Dictionary<OracleDbType, DbType>
        {
            [OracleDbType.BFile] = DbType.Object,
            [OracleDbType.Blob] = DbType.Binary,
            [OracleDbType.Boolean] = DbType.Boolean,
            [OracleDbType.Byte] = DbType.Byte,
            [OracleDbType.Char] = DbType.AnsiStringFixedLength,
            [OracleDbType.Clob] = DbType.AnsiString,
            [OracleDbType.Date] = DbType.DateTime,
            [OracleDbType.Decimal] = DbType.Decimal,
            [OracleDbType.Double] = DbType.Double,
            [OracleDbType.Int16] = DbType.Int16,
            [OracleDbType.Int32] = DbType.Int32,
            [OracleDbType.Int64] = DbType.Int64,
            [OracleDbType.IntervalDS] = DbType.Object,
            [OracleDbType.IntervalYM] = DbType.Object,
            [OracleDbType.Json] = DbType.String,
            [OracleDbType.Long] = DbType.AnsiString,
            [OracleDbType.LongRaw] = DbType.Binary,
            [OracleDbType.NChar] = DbType.StringFixedLength,
            [OracleDbType.NClob] = DbType.String,
            [OracleDbType.NVarchar2] = DbType.String,
            [OracleDbType.Object] = DbType.Object,
            [OracleDbType.Raw] = DbType.Binary,
            [OracleDbType.Single] = DbType.Single,
            [OracleDbType.TimeStamp] = DbType.DateTime2,
            [OracleDbType.TimeStampLTZ] = DbType.DateTimeOffset,
            [OracleDbType.TimeStampTZ] = DbType.DateTimeOffset,
            [OracleDbType.Varchar2] = DbType.AnsiString,
            [OracleDbType.XmlType] = DbType.Xml,
        }.ToFrozenDictionary();

    /// <summary>
    /// Maps an Oracle native data type name to its corresponding <see cref="DbType"/>, provider type, CLR type, and text attributes.
    /// </summary>
    /// <param name="typeName">The Oracle native type name (for example, <c>VARCHAR2</c> or <c>TIMESTAMP(6)</c>).</param>
    /// <returns>
    /// A tuple containing mapped <see cref="DbType"/>, <see cref="OracleDbType"/>, CLR <see cref="Type"/>,
    /// and optional Unicode/fixed-length flags. Unknown types map to object defaults.
    /// </returns>
    public static (DbType DbType, OracleDbType OracleDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapNativeType(string typeName)
    {
        var normalizedTypeName = NormalizeTypeName(typeName);
        if (OracleTypeMappings.TryGetValue(normalizedTypeName, out var mapping))
            return mapping;

        return (DbType.Object, OracleDbType.Object, typeof(object), null, null);
    }

    /// <summary>
    /// Maps a <see cref="DbType"/> value to its closest Oracle-specific <see cref="OracleDbType"/> value.
    /// </summary>
    /// <param name="dbType">The provider-independent database type to map.</param>
    /// <returns>The closest matching <see cref="OracleDbType"/> value. Unknown values map to <see cref="OracleDbType.Object"/>.</returns>
    public static OracleDbType ToOracleDbType(DbType dbType)
    {
        if (DbTypeToOracleDbTypeMappings.TryGetValue(dbType, out var oracleDbType))
            return oracleDbType;

        return OracleDbType.Object;
    }

    /// <summary>
    /// Maps an Oracle-specific <see cref="OracleDbType"/> value to its closest provider-independent <see cref="DbType"/> value.
    /// </summary>
    /// <param name="oracleDbType">The Oracle-specific database type to map.</param>
    /// <returns>The closest matching <see cref="DbType"/> value. Unknown values map to <see cref="DbType.Object"/>.</returns>
    public static DbType ToDbType(OracleDbType oracleDbType)
    {
        if (OracleDbTypeToDbTypeMappings.TryGetValue(oracleDbType, out var dbType))
            return dbType;

        return DbType.Object;
    }

    private static string NormalizeTypeName(string typeName)
    {
        var timestampIndex = typeName.IndexOf("(", StringComparison.Ordinal);
        if (timestampIndex > 0)
            typeName = typeName[..timestampIndex];

        return typeName.Trim().ToUpperInvariant();
    }
}
