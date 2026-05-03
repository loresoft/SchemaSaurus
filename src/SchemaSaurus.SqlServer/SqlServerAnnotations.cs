namespace SchemaSaurus.SqlServer;

/// <summary>
/// Provides well-known SQL Server annotation names used by SchemaSaurus metadata.
/// </summary>
public static class SqlServerAnnotations
{
    /// <summary>
    /// Annotation containing the SQL Server-specific <see cref="System.Data.SqlDbType" /> name for a mapped type.
    /// </summary>
    public const string SqlDbType = "SqlDbType";

    /// <summary>
    /// SQL Server extended property name commonly used for object descriptions.
    /// </summary>
    public const string MsDescription = "MS_Description";
}
