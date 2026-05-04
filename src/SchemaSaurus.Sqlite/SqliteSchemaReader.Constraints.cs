using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Sqlite;

public sealed partial class SqliteSchemaReader
{
    private static async Task ReadPrimaryKeyAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        // WITHOUT ROWID tables expose their primary key as an internal index.
        const string sql = """
            SELECT name
            FROM pragma_index_list($table)
            WHERE origin = 'pk'
            ORDER BY seq
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$table", tableName);

        var name = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (name is null)
        {
            // Rowid tables report primary key columns through pragma_table_info instead.
            await ReadRowidPrimaryKeyAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            return;
        }

        command.Parameters.Clear();
        command.CommandText = """
            SELECT name
            FROM pragma_index_info($index)
            ORDER BY seqno
            """;

        command.Parameters.AddWithValue("$index", name);

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        var columnRefs = new List<ColumnReference>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var columnName = reader.GetString(0);
            var columnReference = new ColumnReference { ColumnName = columnName };

            columnRefs.Add(columnReference);
        }

        // Internal index names are not user-defined constraint names, so replace them with stable metadata names.
        tableBuilder.WithPrimaryKey(
            NormalizeSqliteConstraintName(name, $"pk_{tableName}"),
            isClustered: false,
            [.. columnRefs]);
    }

    private static async Task ReadRowidPrimaryKeyAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        // In pragma_table_info, pk stores the 1-based primary-key column ordinal for rowid tables.
        const string sql = """
            SELECT name
            FROM pragma_table_info($table)
            WHERE pk = 1
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$table", tableName);

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        var columnName = reader.GetString(0);
        var columnReference = new ColumnReference { ColumnName = columnName };

        // Rowid primary keys have no constraint index name to preserve.
        tableBuilder.WithPrimaryKey(
            $"pk_{tableName}",
            isClustered: false,
            columnReference);
    }

    private static async Task ReadUniqueConstraintsAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        // SQLite distinguishes UNIQUE constraints from standalone unique indexes by index origin.
        const string sql = """
            SELECT name
            FROM pragma_index_list($table)
            WHERE origin = 'u'
            ORDER BY seq
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$table", tableName);

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);
        var constraintNames = new List<string>();

        // Buffer names first because each constraint needs a separate pragma_index_info query.
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            string name = reader.GetString(0);

            constraintNames.Add(name);
        }

        // Use an ordinal only when a generated SQLite name must be replaced.
        var uniqueConstraintOrdinal = 1;

        foreach (var constraintName in constraintNames)
        {
            var columns = await ReadIndexColumnsAsync(connection, constraintName, cancellationToken).ConfigureAwait(false);
            if (columns.Count == 0)
            {
                continue;
            }

            // Generated names such as sqlite_autoindex_* should not leak into the provider-neutral model.
            var name = NormalizeSqliteConstraintName(constraintName, $"uq_{tableName}_{uniqueConstraintOrdinal}");

            tableBuilder.AddUniqueConstraint(
                name,
                [.. columns]);

            uniqueConstraintOrdinal++;
        }
    }

    private static async Task<List<string>> ReadPrimaryKeyColumnNamesAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        // Check the WITHOUT ROWID representation first; rowid tables will fall back to pragma_table_info.
        const string indexSql = """
            SELECT name
            FROM pragma_index_list($table)
            WHERE origin = 'pk'
            ORDER BY seq
            """;

        using var command = connection.CreateCommand();
        command.CommandText = indexSql;
        command.Parameters.AddWithValue("$table", tableName);

        var indexName = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (indexName is not null)
        {
            command.Parameters.Clear();
            command.CommandText = """
                SELECT name
                FROM pragma_index_info($index)
                ORDER BY seqno
                """;

            command.Parameters.AddWithValue("$index", indexName);

            return await ReadStringColumnAsync(command, cancellationToken).ConfigureAwait(false);
        }

        // Rowid tables store composite-key order in the pk ordinal column.
        command.Parameters.Clear();
        command.CommandText = """
            SELECT name
            FROM pragma_table_info($table)
            WHERE pk > 0
            ORDER BY pk
            """;

        command.Parameters.AddWithValue("$table", tableName);

        return await ReadStringColumnAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<string>> ReadStringColumnAsync(
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);
        var values = new List<string>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            string item = reader.GetString(0);
            values.Add(item);
        }

        return values;
    }

    // SQLite autoindex names are implementation details, not constraint names from the original DDL.
    private static string NormalizeSqliteConstraintName(string name, string fallbackName)
        => name.StartsWith("sqlite_", StringComparison.Ordinal) ? fallbackName : name;
}
