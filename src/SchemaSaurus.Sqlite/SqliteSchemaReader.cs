using System.Data;

using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Sqlite;

/// <summary>
/// Reads structural metadata from a SQLite database using <c>sqlite_master</c>
/// and <c>PRAGMA</c> statements.
/// </summary>
public sealed partial class SqliteSchemaReader : DatabaseSchemaReader<SqliteConnection>
{
    private const CommandBehavior SequentialResultBehavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;

    // These are the names of the tables and views created by SpatiaLite, a popular spatial extension for SQLite.
    // We need to ignore these when reading the database schema, as they are not user-defined objects.
    private static readonly string[] SpatialiteObjectNames =
    [
        "__EFMigrationsHistory",
        "ElementaryGeometries",
        "geometry_columns",
        "geometry_columns_auth",
        "geometry_columns_field_infos",
        "geometry_columns_statistics",
        "geometry_columns_time",
        "spatial_ref_sys",
        "spatial_ref_sys_aux",
        "SpatialIndex",
        "spatialite_history",
        "sql_statements_log",
        "vector_layers",
        "vector_layers_auth",
        "vector_layers_statistics",
        "vector_layers_field_infos",
        "views_geometry_columns",
        "views_geometry_columns_auth",
        "views_geometry_columns_field_infos",
        "views_geometry_columns_statistics",
        "virts_geometry_columns",
        "virts_geometry_columns_auth",
        "geom_cols_ref_sys",
        "spatial_ref_sys_all",
        "virts_geometry_columns_field_infos",
        "virts_geometry_columns_statistics",
    ];

    private static readonly (string Name, string AnnotationKey)[] DatabasePragmas =
    [
        ("application_id", SqliteAnnotations.ApplicationId),
        ("auto_vacuum", SqliteAnnotations.AutoVacuum),
        ("cache_size", SqliteAnnotations.CacheSize),
        ("foreign_keys", SqliteAnnotations.ForeignKeys),
        ("journal_mode", SqliteAnnotations.JournalMode),
        ("locking_mode", SqliteAnnotations.LockingMode),
        ("page_count", SqliteAnnotations.PageCount),
        ("page_size", SqliteAnnotations.PageSize),
        ("synchronous", SqliteAnnotations.Synchronous),
        ("temp_store", SqliteAnnotations.TempStore),
        ("user_version", SqliteAnnotations.UserVersion),
        ("wal_autocheckpoint", SqliteAnnotations.AutoCheckpoint),
    ];

    /// <inheritdoc />
    public override string ProviderName => "SQLite";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        SqliteConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        const string versionSql = "SELECT sqlite_version()";
        const string encodingSql = "PRAGMA encoding";

        // SQLite has no default schema concept.
        using var command = connection.CreateCommand();
        command.CommandText = versionSql;

        var version = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        builder.WithServerVersion(version);

        command.CommandText = encodingSql;

        var encoding = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        builder
            .WithCollation(encoding)
            .WithEdition("SQLite");

        await ReadDatabasePragmasAsync(command, builder, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReadDatabasePragmasAsync(
        SqliteCommand command,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        foreach (var (name, annotationKey) in DatabasePragmas)
        {
            command.CommandText = "PRAGMA " + name;

            var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            builder.WithAnnotation(annotationKey, value);
        }
    }

    // Note: SQLite does not support sequences, stored procedures, functions, or user-defined types.

    private static bool IsSpatialiteObject(string name)
        => SpatialiteObjectNames.Contains(name, StringComparer.Ordinal);
}
