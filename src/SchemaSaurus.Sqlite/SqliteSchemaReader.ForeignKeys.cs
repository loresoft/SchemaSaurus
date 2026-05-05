using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;

namespace SchemaSaurus.Sqlite;

public sealed partial class SqliteSchemaReader
{
    private static async Task ReadForeignKeysAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        // Each row describes one column mapping, so DISTINCT collapses composite keys to one foreign-key definition.
        const string sql = """
            SELECT DISTINCT id, "table", on_delete, on_update
            FROM pragma_foreign_key_list($table)
            ORDER BY id
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$table", tableName);

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int idOrdinal = 0;
        const int referencedTableOrdinal = 1;
        const int onDeleteOrdinal = 2;
        const int onUpdateOrdinal = 3;

        var foreignKeys = new List<(long Id, string Table, string OnDelete, string OnUpdate)>();

        // Buffer the foreign-key headers before querying their column mappings on the same connection.
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetInt64(idOrdinal);
            var referencedTable = reader.GetString(referencedTableOrdinal);
            var onDelete = reader.GetString(onDeleteOrdinal);
            var onUpdate = reader.GetString(onUpdateOrdinal);

            foreignKeys.Add((id, referencedTable, onDelete, onUpdate));
        }

        foreach (var (id, referencedTable, onDelete, onUpdate) in foreignKeys)
        {
            // The id groups all column mappings that belong to the same foreign key.
            var columns = await ReadForeignKeyColumnsAsync(connection, tableName, referencedTable, id, cancellationToken).ConfigureAwait(false);
            if (columns.Count == 0)
                continue;

            tableBuilder.AddForeignKey(fkBuilder =>
            {
                var foreignKeyName = CreateForeignKeyName(tableName, referencedTable, columns);
                var onUpdateAction = ParseReferentialAction(onUpdate);
                var onDeleteAction = ParseReferentialAction(onDelete);

                fkBuilder
                    .WithName(foreignKeyName)
                    .WithPrincipalTableName(schema: null, referencedTable)
                    .WithOnUpdate(onUpdateAction)
                    .WithOnDelete(onDeleteAction);

                foreach (var (from, to) in columns)
                {
                    fkBuilder.AddColumnMapping(from, to);
                }
            });
        }
    }

    private static async Task<List<(string From, string To)>> ReadForeignKeyColumnsAsync(
        SqliteConnection connection,
        string tableName,
        string referencedTableName,
        long id,
        CancellationToken cancellationToken)
    {
        // seq preserves the declared column order for composite foreign keys.
        const string sql = """
            SELECT seq, "from", "to"
            FROM pragma_foreign_key_list($table)
            WHERE id = $id
            ORDER BY seq
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$table", tableName);
        command.Parameters.AddWithValue("$id", id);

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        var mappings = new List<(string From, string To)>();

        const int sequenceOrdinal = 0;
        const int fromColumnOrdinal = 1;
        const int toColumnOrdinal = 2;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var sequence = reader.GetInt32(sequenceOrdinal);
            var fromColumn = reader.GetString(fromColumnOrdinal);

            // A null "to" column means the foreign key references the corresponding primary-key column.
            var toColumn = reader.GetStringNull(toColumnOrdinal)
                ?? await GetPrimaryKeyColumnNameAsync(connection, referencedTableName, sequence, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(toColumn))
                continue;

            mappings.Add((fromColumn, toColumn!));
        }

        return mappings;
    }

    private static string CreateForeignKeyName(
        string tableName,
        string referencedTableName,
        IReadOnlyList<(string From, string To)> columns)
    {
        var columnNames = string.Join("_", columns.Select(static column => column.From));
        return $"fk_{tableName}_{referencedTableName}_{columnNames}";
    }

    private static async Task<string?> GetPrimaryKeyColumnNameAsync(
        SqliteConnection connection,
        string tableName,
        int sequence,
        CancellationToken cancellationToken)
    {
        var columns = await ReadPrimaryKeyColumnNamesAsync(connection, tableName, cancellationToken).ConfigureAwait(false);
        return sequence >= 0 && sequence < columns.Count ? columns[sequence] : null;
    }

    private static ReferentialAction ParseReferentialAction(string action) => action.ToUpperInvariant() switch
    {
        "CASCADE" => ReferentialAction.Cascade,
        "SET NULL" => ReferentialAction.SetNull,
        "SET DEFAULT" => ReferentialAction.SetDefault,
        "RESTRICT" => ReferentialAction.Restrict,
        _ => ReferentialAction.NoAction,
    };
}
