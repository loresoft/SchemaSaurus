using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Sqlite;

public sealed partial class SqliteSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadTablesAsync(
        SqliteConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableNames = await GetObjectNamesAsync(connection, "table", options, cancellationToken).ConfigureAwait(false);

        foreach (var tableName in tableNames)
        {
            // SQLite does not expose user schemas, so tables are recorded without a schema name.
            var tableBuilder = new TableBuilder()
                .WithQualifiedName(schema: null, tableName);

            // Add dependent table metadata before building the immutable table model.
            await ReadColumnsAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadPrimaryKeyAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadUniqueConstraintsAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadIndexesAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadForeignKeysAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadTriggersAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);

            var table = tableBuilder.Build();
            builder.AddTable(table);
        }
    }

    private static async Task<List<string>> GetObjectNamesAsync(
        SqliteConnection connection,
        string type,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // The query filters out internal SQLite objects (e.g. sqlite_sequence) and applies provider-side filtering based on the options.
        const string sql = """
            SELECT name
            FROM sqlite_master
            WHERE type = $type
              AND instr(name, 'sqlite_') <> 1
            ORDER BY name
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        command.Parameters.AddWithValue("$type", type);

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        var names = new List<string>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.GetString(0);

            // Spatialite creates metadata objects that should not be reported as user tables.
            if (IsSpatialiteObject(name))
            {
                continue;
            }

            // Keep the provider-side filter case-insensitive to match SQLite object lookup behavior.
            if (options.Tables.Count > 0
                && !options.Tables.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            names.Add(name);
        }

        return names;
    }

}
