using System.Data;

namespace SchemaSaurus.Sqlite;

/// <summary>
/// Provides mappings from SQLite declared type names to common .NET data type metadata.
/// </summary>
public static class SqliteTypeMapper
{
    private static readonly string[] SpatialTypeNames =
    [
        "GEOMETRY",
        "GEOGRAPHY",
        "POINT",
        "LINESTRING",
        "POLYGON",
        "MULTIPOINT",
        "MULTILINESTRING",
        "MULTIPOLYGON",
        "GEOMETRYCOLLECTION",
    ];

    /// <summary>
    /// Maps a SQLite declared type name to its corresponding <see cref="DbType" /> and CLR type using SQLite type affinity rules.
    /// </summary>
    /// <param name="typeName">The SQLite declared type name to map (for example, <c>INTEGER</c>, <c>TEXT</c>, or <c>NUMERIC(10,2)</c>).</param>
    /// <returns>
    /// A tuple containing mapped <see cref="DbType" /> and CLR <see cref="Type" />.
    /// Empty or whitespace input maps to binary defaults.
    /// </returns>
    public static (DbType DbType, Type SystemType) MapNativeType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return (DbType.Binary, typeof(byte[]));

        var upper = typeName.ToUpperInvariant();

        if (IsSpatialType(typeName))
            return (DbType.Object, typeof(byte[]));

        // SQLite type affinity rules (https://www.sqlite.org/datatype3.html)
        if (upper.Contains("INT"))
            return (DbType.Int64, typeof(long));

        if (upper.Contains("CHAR") || upper.Contains("CLOB") || upper.Contains("TEXT"))
            return (DbType.String, typeof(string));

        if (upper.Contains("BLOB") || upper.Length == 0)
            return (DbType.Binary, typeof(byte[]));

        if (upper.Contains("REAL") || upper.Contains("FLOA") || upper.Contains("DOUB"))
            return (DbType.Double, typeof(double));

        // NUMERIC affinity (covers NUMERIC, DECIMAL, BOOLEAN, DATE, DATETIME)
        if (upper.Contains("BOOL"))
            return (DbType.Boolean, typeof(bool));

        if (upper.Contains("DATE") || upper.Contains("TIME"))
            return (DbType.DateTime, typeof(DateTime));

        if (upper.Contains("DECIMAL") || upper.Contains("NUMERIC"))
            return (DbType.Decimal, typeof(decimal));

        if (upper.Contains("GUID") || upper.Contains("UUID") || upper.Contains("UNIQUEIDENTIFIER"))
            return (DbType.Guid, typeof(Guid));

        // Default: NUMERIC affinity
        return (DbType.String, typeof(string));
    }

    public static bool IsSpatialType(string typeName)
    {
        var normalizedTypeName = NormalizeTypeName(typeName);
        return SpatialTypeNames.Contains(normalizedTypeName, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeTypeName(string typeName)
    {
        var parenthesisIndex = typeName.IndexOf('(', StringComparison.Ordinal);
        if (parenthesisIndex > 0)
            typeName = typeName[..parenthesisIndex];

        return typeName.Trim();
    }
}
