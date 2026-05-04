using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;

namespace SchemaSaurus.Sqlite;

public sealed partial class SqliteSchemaReader
{
    private static async Task ReadIndexesAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        // origin = 'c' limits results to user-created indexes; constraint-backed indexes are read with constraints.
        const string listSql = """
            SELECT name, "unique"
            FROM pragma_index_list($table)
            WHERE origin = 'c' AND instr(name, 'sqlite_') <> 1
            ORDER BY seq
            """;

        using var listCommand = connection.CreateCommand();
        listCommand.CommandText = listSql;
        listCommand.Parameters.AddWithValue("$table", tableName);

        using var listReader = await listCommand.ExecuteReaderAsync(SingleResultBehavior, cancellationToken).ConfigureAwait(false);

        var indexes = new List<(string Name, bool IsUnique)>();

        const int indexNameOrdinal = 0;
        const int uniqueOrdinal = 1;

        // Buffer index names before reading details because each index needs additional queries on the same connection.
        while (await listReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var indexName = listReader.GetString(indexNameOrdinal);
            var isUnique = listReader.GetInt32(uniqueOrdinal) != 0;

            indexes.Add((indexName, isUnique));
        }

        foreach (var (indexName, isUnique) in indexes)
        {
            var indexBuilder = new IndexBuilder()
                .WithName(indexName)
                .WithIsUnique(isUnique);

            const string indexSqlCommandText = """
                SELECT sql FROM sqlite_master
                WHERE type = 'index' AND name = $name
                """;

            // Partial indexes store their filter in sqlite_master.sql rather than a pragma column.
            using var sqlCommand = connection.CreateCommand();
            sqlCommand.CommandText = indexSqlCommandText;
            sqlCommand.Parameters.AddWithValue("$name", indexName);

            var indexSql = (string?)await sqlCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            if (indexSql is not null && indexSql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                indexBuilder.WithIsFiltered(true);

                var whereIndex = indexSql.LastIndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
                indexBuilder.WithFilterExpression(indexSql[(whereIndex + 5)..].Trim());
            }

            var columns = await ReadIndexColumnsAsync(connection, indexName, cancellationToken).ConfigureAwait(false);
            foreach (var column in columns)
            {
                indexBuilder.AddColumn(column.ColumnName, column.SortDirection);
            }

            var index = indexBuilder.Build();
            tableBuilder.AddIndex(index);
        }
    }

    private static async Task<List<ColumnReference>> ReadIndexColumnsAsync(
        SqliteConnection connection,
        string indexName,
        CancellationToken cancellationToken)
    {
        // pragma_index_xinfo includes hidden columns; key = 1 keeps only indexed key columns.
        const string sql = """
            SELECT name, "desc"
            FROM pragma_index_xinfo($index)
            WHERE key = 1
            ORDER BY seqno
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$index", indexName);

        using var reader = await command.ExecuteReaderAsync(SingleResultBehavior, cancellationToken).ConfigureAwait(false);

        var columns = new List<ColumnReference>();
        const int columnNameOrdinal = 0;
        const int descendingOrdinal = 1;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Expression indexes have no column name and cannot be represented as ColumnReference values.
            var columnName = reader.GetStringNull(columnNameOrdinal);
            if (columnName is null)
                continue;

            var sortDirection = reader.GetInt32(descendingOrdinal) != 0
                ? SortDirection.Descending
                : SortDirection.Ascending;

            var columnReference = new ColumnReference
            {
                ColumnName = columnName,
                SortDirection = sortDirection,
            };

            columns.Add(columnReference);
        }

        return columns;
    }
}
