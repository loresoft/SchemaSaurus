using MySqlConnector;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.MySql;

public sealed partial class MySqlSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadTablesAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tables = await ReadTableDefinitionsAsync(connection, options, cancellationToken).ConfigureAwait(false);
        if (tables.Count == 0)
            return;

        await ReadRelationColumnsAsync(connection, tables, "= 'BASE TABLE'", options, cancellationToken).ConfigureAwait(false);
        await ReadTableConstraintsAsync(connection, tables, options, cancellationToken).ConfigureAwait(false);
        await ReadTableIndexesAsync(connection, tables, options, cancellationToken).ConfigureAwait(false);
        await ReadTableTriggersAsync(connection, tables, options, cancellationToken).ConfigureAwait(false);

        foreach (var (_, tableBuilder) in tables)
        {
            var table = tableBuilder.Build();
            builder.AddTable(table);
        }
    }

    private static async Task<Dictionary<(string Schema, string Name), TableBuilder>> ReadTableDefinitionsAsync(
        MySqlConnection connection,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableFilter = BuildInformationSchemaTableFilter(options, "t.TABLE_SCHEMA", "t.TABLE_NAME");

        var sql = $"""
            SELECT
                t.TABLE_SCHEMA,
                t.TABLE_NAME,
                t.TABLE_COMMENT,
                t.ENGINE,
                t.TABLE_COLLATION,
                ccsa.CHARACTER_SET_NAME
            FROM INFORMATION_SCHEMA.TABLES t
            LEFT JOIN INFORMATION_SCHEMA.COLLATION_CHARACTER_SET_APPLICABILITY ccsa
                ON ccsa.COLLATION_NAME = t.TABLE_COLLATION
            WHERE t.TABLE_TYPE = 'BASE TABLE'
              AND {tableFilter}
            ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int nameOrdinal = 1;
        const int commentOrdinal = 2;
        const int engineOrdinal = 3;
        const int collationOrdinal = 4;
        const int characterSetOrdinal = 5;

        var tables = new Dictionary<(string Schema, string Name), TableBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var comment = NullIfEmpty(reader.GetStringNull(commentOrdinal));
            var engine = reader.GetStringNull(engineOrdinal);
            var collation = reader.GetStringNull(collationOrdinal);
            var characterSet = reader.GetStringNull(characterSetOrdinal);

            var tableBuilder = new TableBuilder()
                .WithSchemaQualifiedName(schema, name)
                .WithDescription(comment)
                .WithAnnotation(MySqlAnnotations.Engine, engine)
                .WithAnnotation(MySqlAnnotations.CharacterSet, characterSet)
                .WithAnnotation("Collation", collation);

            tables[(schema, name)] = tableBuilder;
        }

        return tables;
    }

    private static async Task ReadTableConstraintsAsync(
        MySqlConnection connection,
        Dictionary<(string Schema, string Name), TableBuilder> tables,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableFilter = BuildInformationSchemaTableFilter(options, "tc.TABLE_SCHEMA", "tc.TABLE_NAME");
        var sql = $"""
            SELECT
                tc.TABLE_SCHEMA,
                tc.TABLE_NAME,
                tc.CONSTRAINT_NAME,
                tc.CONSTRAINT_TYPE,
                kcu.COLUMN_NAME,
                kcu.REFERENCED_TABLE_SCHEMA,
                kcu.REFERENCED_TABLE_NAME,
                kcu.REFERENCED_COLUMN_NAME,
                rc.DELETE_RULE,
                rc.UPDATE_RULE
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
                AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
                AND kcu.TABLE_NAME = tc.TABLE_NAME
            LEFT JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                ON rc.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
                AND rc.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
            WHERE tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE', 'FOREIGN KEY')
              AND {tableFilter}
            ORDER BY tc.TABLE_SCHEMA, tc.TABLE_NAME, tc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int tableOrdinal = 1;
        const int constraintOrdinal = 2;
        const int typeOrdinal = 3;
        const int columnOrdinal = 4;
        const int principalSchemaOrdinal = 5;
        const int principalTableOrdinal = 6;
        const int principalColumnOrdinal = 7;
        const int deleteOrdinal = 8;
        const int updateOrdinal = 9;

        var primaryKeys = new Dictionary<(string Schema, string Table, string Name), List<ColumnReference>>();
        var uniqueConstraints = new Dictionary<(string Schema, string Table, string Name), List<ColumnReference>>();
        var foreignKeys = new Dictionary<(string Schema, string Table, string Name), ForeignKeyBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var tableName = reader.GetString(tableOrdinal);
            if (!tables.ContainsKey((schema, tableName)))
                continue;

            var constraintName = reader.GetString(constraintOrdinal);
            var constraintType = reader.GetString(typeOrdinal);
            var columnName = reader.GetStringNull(columnOrdinal);
            if (columnName is null)
                continue;

            var key = (schema, tableName, constraintName);
            ColumnReference reference = new()
            {
                ColumnName = columnName,
            };

            if (constraintType == "PRIMARY KEY")
            {
                if (!primaryKeys.TryGetValue(key, out var columns))
                {
                    columns = [];
                    primaryKeys[key] = columns;
                }

                columns.Add(reference);
            }
            else if (constraintType == "UNIQUE")
            {
                if (!uniqueConstraints.TryGetValue(key, out var columns))
                {
                    columns = [];
                    uniqueConstraints[key] = columns;
                }

                columns.Add(reference);
            }
            else
            {
                var principalSchema = reader.GetStringNull(principalSchemaOrdinal);
                var principalTable = reader.GetStringNull(principalTableOrdinal);
                var principalColumn = reader.GetStringNull(principalColumnOrdinal);
                if (principalSchema is null || principalTable is null || principalColumn is null)
                    continue;

                if (!foreignKeys.TryGetValue(key, out var foreignKeyBuilder))
                {
                    foreignKeyBuilder = new ForeignKeyBuilder()
                        .WithName(constraintName)
                        .WithPrincipalTableName(principalSchema, principalTable)
                        .WithOnDelete(MapReferentialAction(reader.GetStringNull(deleteOrdinal)))
                        .WithOnUpdate(MapReferentialAction(reader.GetStringNull(updateOrdinal)));

                    foreignKeys[key] = foreignKeyBuilder;
                }

                foreignKeyBuilder.AddColumnMapping(columnName, principalColumn);
            }
        }

        foreach (var ((schema, tableName, name), columns) in primaryKeys)
        {
            var primaryKeyName = NormalizePrimaryKeyName(name, tableName);
            tables[(schema, tableName)].WithPrimaryKey(primaryKeyName, false, [.. columns]);
        }

        foreach (var ((schema, tableName, name), columns) in uniqueConstraints)
            tables[(schema, tableName)].AddUniqueConstraint(name, [.. columns]);

        foreach (var ((schema, tableName, _), foreignKeyBuilder) in foreignKeys)
        {
            var foreignKey = foreignKeyBuilder.Build();
            tables[(schema, tableName)].AddForeignKey(foreignKey);
        }
    }

    private static async Task ReadTableIndexesAsync(
        MySqlConnection connection,
        Dictionary<(string Schema, string Name), TableBuilder> tables,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableFilter = BuildInformationSchemaTableFilter(options, "s.TABLE_SCHEMA", "s.TABLE_NAME");

        var sql = $"""
            SELECT
                s.TABLE_SCHEMA,
                s.TABLE_NAME,
                s.INDEX_NAME,
                s.NON_UNIQUE,
                s.COLUMN_NAME,
                s.COLLATION,
                s.INDEX_TYPE,
                s.SUB_PART
            FROM INFORMATION_SCHEMA.STATISTICS s
            WHERE s.INDEX_NAME <> 'PRIMARY'
              AND {tableFilter}
            ORDER BY s.TABLE_SCHEMA, s.TABLE_NAME, s.INDEX_NAME, s.SEQ_IN_INDEX
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int tableOrdinal = 1;
        const int nameOrdinal = 2;
        const int nonUniqueOrdinal = 3;
        const int columnOrdinal = 4;
        const int collationOrdinal = 5;
        const int indexTypeOrdinal = 6;
        const int prefixLengthOrdinal = 7;

        var indexes = new Dictionary<(string Schema, string Table, string Name), IndexBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var tableName = reader.GetString(tableOrdinal);
            if (!tables.ContainsKey((schema, tableName)))
                continue;

            var indexName = reader.GetString(nameOrdinal);
            var key = (schema, tableName, indexName);
            if (!indexes.TryGetValue(key, out var indexBuilder))
            {
                var isUnique = reader.GetInt64(nonUniqueOrdinal) == 0;
                var indexType = reader.GetStringNull(indexTypeOrdinal);

                indexBuilder = new IndexBuilder()
                    .WithName(indexName)
                    .WithIsUnique(isUnique)
                    .WithIndexType(indexType);

                if (string.Equals(indexType, "FULLTEXT", StringComparison.OrdinalIgnoreCase))
                    indexBuilder.WithAnnotation(MySqlAnnotations.FullTextIndex, true);

                if (string.Equals(indexType, "SPATIAL", StringComparison.OrdinalIgnoreCase))
                    indexBuilder.WithAnnotation(MySqlAnnotations.SpatialIndex, true);

                indexes[key] = indexBuilder;
            }

            var columnName = reader.GetStringNull(columnOrdinal);
            if (columnName is null)
                continue;

            var sortDirection = reader.GetStringNull(collationOrdinal) == "D"
                ? SortDirection.Descending
                : SortDirection.Ascending;

            var prefixLength = GetInt32Null(reader.GetValueNull(prefixLengthOrdinal));
            indexBuilder.AddColumn(columnName, sortDirection);

            if (prefixLength is not null)
            {
                var annotationName = MySqlAnnotations.IndexPrefixLength + ":" + columnName;
                indexBuilder.WithAnnotation(annotationName, prefixLength);
            }
        }

        foreach (var ((schema, tableName, _), indexBuilder) in indexes)
        {
            var index = indexBuilder.Build();
            tables[(schema, tableName)].AddIndex(index);
        }
    }

    private static async Task ReadTableTriggersAsync(
        MySqlConnection connection,
        Dictionary<(string Schema, string Name), TableBuilder> tables,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableFilter = BuildInformationSchemaTableFilter(options, "tr.EVENT_OBJECT_SCHEMA", "tr.EVENT_OBJECT_TABLE");

        var sql = $"""
            SELECT
                tr.EVENT_OBJECT_SCHEMA,
                tr.EVENT_OBJECT_TABLE,
                tr.TRIGGER_NAME,
                tr.ACTION_TIMING,
                tr.EVENT_MANIPULATION,
                tr.ACTION_STATEMENT
            FROM INFORMATION_SCHEMA.TRIGGERS tr
            WHERE {tableFilter}
            ORDER BY tr.EVENT_OBJECT_SCHEMA, tr.EVENT_OBJECT_TABLE, tr.TRIGGER_NAME
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int tableOrdinal = 1;
        const int nameOrdinal = 2;
        const int timingOrdinal = 3;
        const int eventOrdinal = 4;
        const int definitionOrdinal = 5;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var tableName = reader.GetString(tableOrdinal);
            if (!tables.TryGetValue((schema, tableName), out var tableBuilder))
                continue;

            var timing = reader.GetString(timingOrdinal).Equals("BEFORE", StringComparison.OrdinalIgnoreCase)
                ? TriggerTiming.Before
                : TriggerTiming.After;

            var events = reader.GetString(eventOrdinal).ToUpperInvariant() switch
            {
                "INSERT" => TriggerEvent.Insert,
                "UPDATE" => TriggerEvent.Update,
                "DELETE" => TriggerEvent.Delete,
                _ => TriggerEvent.None,
            };

            Trigger trigger = new()
            {
                Name = reader.GetString(nameOrdinal),
                Timing = timing,
                Events = events,
                Definition = reader.GetStringNull(definitionOrdinal),
            };

            tableBuilder.AddTrigger(trigger);
        }
    }

    private static string NormalizePrimaryKeyName(string name, string tableName)
        => name.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase) ? $"pk_{tableName}" : name;
}
