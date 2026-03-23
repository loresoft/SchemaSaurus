using System.Data;
using System.Globalization;

using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Sqlite;

/// <summary>
/// Reads structural metadata from a SQLite database using <c>sqlite_master</c>
/// and <c>PRAGMA</c> statements.
/// </summary>
public sealed class SqliteSchemaReader : DatabaseSchemaReader<SqliteConnection>
{
    /// <inheritdoc />
    public override string ProviderName => "SQLite";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        SqliteConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        // SQLite has no default schema concept.
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT sqlite_version()";
        var version = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        builder.WithServerVersion(version);

        command.CommandText = "PRAGMA encoding";
        var encoding = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        builder.WithCollation(encoding);
    }

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
            var tableBuilder = new TableBuilder()
                .WithSchemaQualifiedName(schema: null, tableName);

            await ReadColumnsAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadPrimaryKeyAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadIndexesAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadForeignKeysAsync(connection, tableName, tableBuilder, cancellationToken).ConfigureAwait(false);
            await ReadTriggersAsync(connection, tableName, options, tableBuilder, cancellationToken).ConfigureAwait(false);

            builder.AddTable(tableBuilder.Build());
        }
    }

    /// <inheritdoc />
    protected override async Task ReadViewsAsync(
        SqliteConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'view' ORDER BY name";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var viewName = reader.GetString(0);
            var sql = reader.IsDBNull(1) ? null : reader.GetString(1);

            var viewBuilder = new ViewBuilder()
                .WithSchemaQualifiedName(schema: null, viewName);

            if (options.IncludeDefinitions)
                viewBuilder.WithDefinition(sql);

            await ReadViewColumnsAsync(connection, viewName, viewBuilder, cancellationToken).ConfigureAwait(false);

            builder.AddView(viewBuilder.Build());
        }
    }

    // Note: SQLite does not support sequences, stored procedures, functions, or user-defined types.

    private static async Task<List<string>> GetObjectNamesAsync(
        SqliteConnection connection,
        string type,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = $type
              AND name NOT LIKE 'sqlite_%'
            ORDER BY name
            """;

        command.Parameters.AddWithValue("$type", type);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var names = new List<string>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.GetString(0);

            if (options.Tables.Count > 0
                && !options.Tables.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            names.Add(name);
        }

        return names;
    }

    private static async Task ReadColumnsAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        // table_xinfo includes hidden columns and generated columns
        command.CommandText = $"PRAGMA table_xinfo(\"{EscapeIdentifier(tableName)}\")";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // cid | name | type | notnull | dflt_value | pk | hidden
            var ordinal = reader.GetInt32(0);
            var columnName = reader.GetString(1);
            var typeName = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var notNull = reader.GetInt32(3) != 0;
            var defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4);
            var pkFlag = reader.GetInt32(5);
            var hidden = reader.GetInt32(6); // 0=normal, 1=hidden, 2=generated virtual, 3=generated stored

            var (dbType, systemType) = MapSqliteType(typeName);

            tableBuilder.AddColumn(col => col
                .WithName(columnName)
                .WithOrdinalPosition(ordinal + 1) // convert 0-based cid to 1-based ordinal
                .WithIsNullable(!notNull && pkFlag == 0)
                .WithDefaultValueSql(defaultValue)
                .WithIsIdentity(IsRowIdAlias(typeName, pkFlag))
                .WithIsComputed(hidden is 2 or 3)
                .WithIsStored(hidden == 3)
                .WithNativeTypeName(string.IsNullOrEmpty(typeName) ? "BLOB" : typeName)
                .WithDbType(dbType)
                .WithSystemType(systemType));
        }
    }

    private static async Task ReadPrimaryKeyAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{EscapeIdentifier(tableName)}\")";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var pkColumns = new SortedList<int, string>(); // pk ordinal -> column name

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // cid | name | type | notnull | dflt_value | pk
            var columnName = reader.GetString(1);
            var pk = reader.GetInt32(5);

            if (pk > 0)
                pkColumns.Add(pk, columnName);
        }

        if (pkColumns.Count > 0)
        {
            var columnRefs = pkColumns.Values
                .Select(name => new ColumnReference { ColumnName = name })
                .ToArray();

            tableBuilder.WithPrimaryKey(
                $"pk_{tableName}",
                isClustered: false,
                columnRefs);
        }
    }

    private static async Task ReadIndexesAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        using var listCommand = connection.CreateCommand();
        listCommand.CommandText = $"PRAGMA index_list(\"{EscapeIdentifier(tableName)}\")";

        using var listReader = await listCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var indexes = new List<(string Name, bool IsUnique, string Origin)>();
        while (await listReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // seq | name | unique | origin | partial
            var indexName = listReader.GetString(1);
            var isUnique = listReader.GetInt32(2) != 0;
            var origin = listReader.GetString(3); // 'c' = CREATE INDEX, 'u' = UNIQUE constraint, 'pk' = primary key

            // Skip auto-generated primary key indexes; we handle PK separately.
            if (origin == "pk")
                continue;

            indexes.Add((indexName, isUnique, origin));
        }

        foreach (var (indexName, isUnique, origin) in indexes)
        {
            var indexBuilder = new IndexBuilder()
                .WithName(indexName)
                .WithIsUnique(isUnique);

            // Check for partial index
            using var sqlCommand = connection.CreateCommand();
            sqlCommand.CommandText = """
                SELECT sql FROM sqlite_master
                WHERE type = 'index' AND name = $name
                """;
            sqlCommand.Parameters.AddWithValue("$name", indexName);

            var indexSql = (string?)await sqlCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (indexSql is not null && indexSql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                indexBuilder.WithIsFiltered(true);
                var whereIndex = indexSql.LastIndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
                indexBuilder.WithFilterExpression(indexSql[(whereIndex + 5)..].Trim());
            }

            using var infoCommand = connection.CreateCommand();
            infoCommand.CommandText = $"PRAGMA index_info(\"{EscapeIdentifier(indexName)}\")";

            using var infoReader = await infoCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await infoReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // seqno | cid | name
                var columnName = infoReader.GetString(2);
                indexBuilder.AddColumn(columnName);
            }

            tableBuilder.AddIndex(indexBuilder.Build());
        }
    }

    private static async Task ReadForeignKeysAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list(\"{EscapeIdentifier(tableName)}\")";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        // Group by id since composite FKs share the same id
        var fkMap = new Dictionary<int, (string Table, List<(string From, string To)> Columns, string OnUpdate, string OnDelete)>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // id | seq | table | from | to | on_update | on_delete | match
            var id = reader.GetInt32(0);
            var referencedTable = reader.GetString(2);
            var fromColumn = reader.GetString(3);
            var toColumn = reader.GetString(4);
            var onUpdate = reader.GetString(5);
            var onDelete = reader.GetString(6);

            if (!fkMap.TryGetValue(id, out var fk))
            {
                fk = (referencedTable, [], onUpdate, onDelete);
                fkMap[id] = fk;
            }

            fk.Columns.Add((fromColumn, toColumn));
        }

        foreach (var (id, (referencedTable, columns, onUpdate, onDelete)) in fkMap)
        {
            tableBuilder.AddForeignKey(fkBuilder =>
            {
                fkBuilder
                    .WithName($"fk_{tableName}_{id}")
                    .WithPrincipalTableName(schema: null, referencedTable)
                    .WithOnUpdate(ParseReferentialAction(onUpdate))
                    .WithOnDelete(ParseReferentialAction(onDelete));

                foreach (var (from, to) in columns)
                    fkBuilder.AddColumnMapping(from, to);
            });
        }
    }

    private static async Task ReadTriggersAsync(
        SqliteConnection connection,
        string tableName,
        SchemaReaderOptions options,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name, sql
            FROM sqlite_master
            WHERE type = 'trigger' AND tbl_name = $tableName
            ORDER BY name
            """;
        command.Parameters.AddWithValue("$tableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var triggerName = reader.GetString(0);
            var sql = reader.IsDBNull(1) ? null : reader.GetString(1);

            var (timing, events) = ParseTriggerSql(sql);

            var trigger = new Trigger
            {
                Name = triggerName,
                Timing = timing,
                Events = events,
                Definition = options.IncludeDefinitions ? sql : null,
            };

            tableBuilder.AddTrigger(trigger);
        }
    }

    private static async Task ReadViewColumnsAsync(
        SqliteConnection connection,
        string viewName,
        ViewBuilder viewBuilder,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{EscapeIdentifier(viewName)}\")";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // cid | name | type | notnull | dflt_value | pk
            var ordinal = reader.GetInt32(0);
            var columnName = reader.GetString(1);
            var typeName = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var notNull = reader.GetInt32(3) != 0;

            var (dbType, systemType) = MapSqliteType(typeName);

            viewBuilder.AddColumn(col => col
                .WithName(columnName)
                .WithOrdinalPosition(ordinal + 1)
                .WithIsNullable(!notNull)
                .WithNativeTypeName(string.IsNullOrEmpty(typeName) ? "BLOB" : typeName)
                .WithDbType(dbType)
                .WithSystemType(systemType));
        }
    }

    /// <summary>
    /// Maps a SQLite declared type to a <see cref="DbType"/> and CLR <see cref="Type"/>
    /// using the SQLite type affinity rules.
    /// </summary>
    private static (DbType DbType, Type SystemType) MapSqliteType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return (DbType.Binary, typeof(byte[]));

        var upper = typeName.ToUpperInvariant();

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

    /// <summary>
    /// Determines whether a column declared as INTEGER PRIMARY KEY is a rowid alias (identity).
    /// </summary>
    private static bool IsRowIdAlias(string typeName, int pkFlag)
        => pkFlag > 0
           && typeName.Equals("INTEGER", StringComparison.OrdinalIgnoreCase);

    private static ReferentialAction ParseReferentialAction(string action) => action.ToUpperInvariant() switch
    {
        "CASCADE" => ReferentialAction.Cascade,
        "SET NULL" => ReferentialAction.SetNull,
        "SET DEFAULT" => ReferentialAction.SetDefault,
        "RESTRICT" => ReferentialAction.Restrict,
        _ => ReferentialAction.NoAction,
    };

    private static (TriggerTiming Timing, TriggerEvent Events) ParseTriggerSql(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return (TriggerTiming.Before, TriggerEvent.None);

        var upper = sql.ToUpperInvariant();

        var timing = TriggerTiming.Before;
        if (upper.Contains("INSTEAD OF"))
            timing = TriggerTiming.InsteadOf;
        else if (upper.Contains("AFTER"))
            timing = TriggerTiming.After;

        var events = TriggerEvent.None;
        if (upper.Contains("INSERT"))
            events |= TriggerEvent.Insert;
        if (upper.Contains("UPDATE"))
            events |= TriggerEvent.Update;
        if (upper.Contains("DELETE"))
            events |= TriggerEvent.Delete;

        return (timing, events);
    }

    private static string EscapeIdentifier(string identifier)
        => identifier.Replace("\"", "\"\"", StringComparison.Ordinal);
}
