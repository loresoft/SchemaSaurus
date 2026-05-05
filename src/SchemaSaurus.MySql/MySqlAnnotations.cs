namespace SchemaSaurus.MySql;

/// <summary>
/// Provides well-known MySQL annotation names used by SchemaSaurus metadata.
/// </summary>
public static class MySqlAnnotations
{
    /// <summary>
    /// Annotation containing the MySQL-specific <see cref="MySqlConnector.MySqlDbType" /> name for a mapped type.
    /// </summary>
    public const string MySqlDbType = "MySqlDbType";

    /// <summary>
    /// Annotation containing the storage engine for a table.
    /// </summary>
    public const string Engine = "Engine";

    /// <summary>
    /// Annotation containing the default character set for a database object or column.
    /// </summary>
    public const string CharacterSet = "CharacterSet";

    /// <summary>
    /// Annotation containing the prefix length for an indexed column.
    /// </summary>
    public const string IndexPrefixLength = "IndexPrefixLength";

    /// <summary>
    /// Annotation indicating a FULLTEXT index.
    /// </summary>
    public const string FullTextIndex = "FullTextIndex";

    /// <summary>
    /// Annotation indicating a SPATIAL index.
    /// </summary>
    public const string SpatialIndex = "SpatialIndex";

    /// <summary>
    /// Annotation containing a spatial reference system identifier.
    /// </summary>
    public const string SpatialReferenceSystemId = "SpatialReferenceSystemId";
}
