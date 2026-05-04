namespace SchemaSaurus.PostgreSQL;

/// <summary>
/// Provides well-known PostgreSQL annotation names used by SchemaSaurus metadata.
/// </summary>
public static class PostgreSqlAnnotations
{
    /// <summary>
    /// Annotation containing the PostgreSQL-specific <see cref="NpgsqlTypes.NpgsqlDbType" /> name for a mapped type.
    /// </summary>
    public const string NpgsqlDbType = "NpgsqlDbType";

    /// <summary>
    /// Prefix for annotations containing PostgreSQL storage parameter values.
    /// </summary>
    public const string StorageParameterPrefix = "StorageParameter:";
}
