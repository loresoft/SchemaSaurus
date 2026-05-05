using Npgsql;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSql;

public sealed partial class PostgreSqlSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadTablesAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadTablesCoreAsync(connection, builder, options, cancellationToken);
    }

    private async Task ReadTablesCoreAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableFilter = BuildTableFilter(options, "ns.nspname", "cls.relname");

        var tables = await ReadTableDefinitionsAsync(connection, tableFilter, cancellationToken).ConfigureAwait(false);
        if (tables.Count == 0)
            return;

        await ReadRelationColumnsAsync(connection, tables, tableFilter, cancellationToken).ConfigureAwait(false);
        await ReadTableConstraintsAsync(connection, tables, cancellationToken).ConfigureAwait(false);
        await ReadTableIndexesAsync(connection, tables, cancellationToken).ConfigureAwait(false);
        await ReadTableTriggersAsync(connection, tables, cancellationToken).ConfigureAwait(false);

        foreach (var (_, tableBuilder) in tables)
        {
            var table = tableBuilder.Build();
            builder.AddTable(table);
        }
    }

    private static async Task<Dictionary<uint, TableBuilder>> ReadTableDefinitionsAsync(
        NpgsqlConnection connection,
        string tableFilter,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                cls.oid,
                ns.nspname,
                cls.relname,
                des.description,
                cls.reloptions
            FROM pg_class AS cls
            JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
            LEFT JOIN pg_description AS des ON des.objoid = cls.oid AND des.objsubid = 0
            WHERE cls.relkind IN ('r', 'p', 'f')
              AND {tableFilter}
              AND NOT EXISTS (
                  SELECT 1
                  FROM pg_depend dep
                  WHERE dep.objid = cls.oid AND dep.deptype IN ('e', 'x')
              )
            ORDER BY ns.nspname, cls.relname
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int schemaOrdinal = 1;
        const int nameOrdinal = 2;
        const int descriptionOrdinal = 3;
        const int optionsOrdinal = 4;

        var tables = new Dictionary<uint, TableBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetFieldValue<uint>(objectIdOrdinal);
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var description = reader.GetStringNull(descriptionOrdinal);
            var storageParameters = reader.GetFieldValueNull<string[]>(optionsOrdinal);

            var tableBuilder = new TableBuilder()
                .WithSchemaQualifiedName(schema, name)
                .WithDescription(description);

            AddStorageParameterAnnotations(tableBuilder, storageParameters);
            tables[objectId] = tableBuilder;
        }

        return tables;
    }

    private static async Task ReadTableConstraintsAsync(
        NpgsqlConnection connection,
        Dictionary<uint, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        var objectIds = tables.Keys.ToHashSet();

        const string sql = """
            SELECT
                con.conrelid,
                con.conname,
                con.contype::text,
                frnns.nspname,
                frncls.relname,
                con.confdeltype::text,
                con.confupdtype::text,
                ARRAY(
                    SELECT attr.attname
                    FROM unnest(con.conkey) WITH ORDINALITY AS column_key(attnum, ordinal_position)
                    JOIN pg_attribute AS attr ON attr.attrelid = con.conrelid AND attr.attnum = column_key.attnum
                    ORDER BY column_key.ordinal_position
                ) AS dependent_columns,
                ARRAY(
                    SELECT attr.attname
                    FROM unnest(con.confkey) WITH ORDINALITY AS column_key(attnum, ordinal_position)
                    JOIN pg_attribute AS attr ON attr.attrelid = con.confrelid AND attr.attnum = column_key.attnum
                    ORDER BY column_key.ordinal_position
                ) AS principal_columns
            FROM pg_constraint AS con
            JOIN pg_class AS cls ON cls.oid = con.conrelid
            JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
            LEFT JOIN pg_class AS frncls ON frncls.oid = con.confrelid
            LEFT JOIN pg_namespace AS frnns ON frnns.oid = frncls.relnamespace
            WHERE con.contype IN ('p', 'u', 'f')
              AND cls.relkind IN ('r', 'p', 'f')
            ORDER BY con.conrelid, con.conname
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int nameOrdinal = 1;
        const int typeOrdinal = 2;
        const int principalSchemaOrdinal = 3;
        const int principalTableOrdinal = 4;
        const int deleteOrdinal = 5;
        const int updateOrdinal = 6;
        const int dependentColumnsOrdinal = 7;
        const int principalColumnsOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetFieldValue<uint>(objectIdOrdinal);
            if (!objectIds.Contains(objectId))
                continue;

            var name = reader.GetString(nameOrdinal);
            var type = reader.GetString(typeOrdinal);
            var principalSchema = reader.GetStringNull(principalSchemaOrdinal);
            var principalTable = reader.GetStringNull(principalTableOrdinal);
            var deleteAction = reader.GetStringNull(deleteOrdinal);
            var updateAction = reader.GetStringNull(updateOrdinal);
            var dependentColumns = reader.GetFieldValueNull<string[]>(dependentColumnsOrdinal) ?? [];
            var principalColumns = reader.GetFieldValueNull<string[]>(principalColumnsOrdinal) ?? [];

            var tableBuilder = tables[objectId];
            var columnNames = dependentColumns;

            if (type == "p")
            {
                var references = CreateColumnReferences(columnNames);
                tableBuilder.WithPrimaryKey(name, false, references);
            }
            else if (type == "u")
            {
                var references = CreateColumnReferences(columnNames);
                tableBuilder.AddUniqueConstraint(name, references);
            }
            else if (principalSchema is not null && principalTable is not null)
            {
                var principalColumnNames = principalColumns;

                var foreignKeyBuilder = new ForeignKeyBuilder()
                    .WithName(name)
                    .WithPrincipalTableName(principalSchema, principalTable)
                    .WithOnDelete(MapReferentialAction(deleteAction))
                    .WithOnUpdate(MapReferentialAction(updateAction));

                for (var i = 0; i < columnNames.Length && i < principalColumnNames.Length; i++)
                {
                    var dependentColumnName = columnNames[i];
                    var principalColumnName = principalColumnNames[i];

                    foreignKeyBuilder.AddColumnMapping(dependentColumnName, principalColumnName);
                }

                var foreignKey = foreignKeyBuilder.Build();
                tableBuilder.AddForeignKey(foreignKey);
            }
        }
    }

    private static async Task ReadTableIndexesAsync(
        NpgsqlConnection connection,
        Dictionary<uint, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        var objectIds = tables.Keys.ToHashSet();

        const string sql = """
            SELECT
                cls.oid,
                idxcls.oid,
                idxcls.relname,
                idx.indisunique,
                idx.indisprimary,
                am.amname,
                ARRAY(
                    SELECT attr.attname
                    FROM unnest(idx.indkey, idx.indoption) WITH ORDINALITY AS key_column(attnum, option, ordinal_position)
                    LEFT JOIN pg_attribute AS attr ON attr.attrelid = cls.oid AND attr.attnum = key_column.attnum
                    WHERE key_column.attnum > 0
                    ORDER BY key_column.ordinal_position
                ) AS columns,
                ARRAY(
                    SELECT (key_column.option & 1) <> 0
                    FROM unnest(idx.indkey, idx.indoption) WITH ORDINALITY AS key_column(attnum, option, ordinal_position)
                    WHERE key_column.attnum > 0
                    ORDER BY key_column.ordinal_position
                ) AS descending_columns,
                ARRAY(
                    SELECT pg_get_indexdef(idx.indexrelid, key_column.ordinal_position::integer, false)
                    FROM unnest(idx.indkey) WITH ORDINALITY AS key_column(attnum, ordinal_position)
                    WHERE key_column.attnum = 0
                    ORDER BY key_column.ordinal_position
                ) AS expressions,
                CASE WHEN idx.indpred IS NULL THEN NULL ELSE pg_get_expr(idx.indpred, cls.oid) END AS predicate,
                idxcls.reloptions
            FROM pg_class AS cls
            JOIN pg_index AS idx ON idx.indrelid = cls.oid
            JOIN pg_class AS idxcls ON idxcls.oid = idx.indexrelid
            JOIN pg_am AS am ON am.oid = idxcls.relam
            WHERE cls.relkind IN ('r', 'p', 'f')
              AND NOT idx.indisprimary
              AND NOT EXISTS (
                  SELECT 1
                  FROM pg_constraint con
                  WHERE con.conindid = idx.indexrelid AND con.contype IN ('p', 'u')
              )
            ORDER BY cls.oid, idxcls.relname
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int nameOrdinal = 2;
        const int uniqueOrdinal = 3;
        const int methodOrdinal = 5;
        const int columnsOrdinal = 6;
        const int descendingOrdinal = 7;
        const int expressionsOrdinal = 8;
        const int predicateOrdinal = 9;
        const int optionsOrdinal = 10;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetFieldValue<uint>(objectIdOrdinal);
            if (!objectIds.Contains(objectId))
                continue;

            var indexName = reader.GetString(nameOrdinal);
            var isUnique = reader.GetBoolean(uniqueOrdinal);
            var indexType = reader.GetString(methodOrdinal);
            var columns = reader.GetFieldValue<string[]>(columnsOrdinal);
            var descendingColumns = reader.GetFieldValue<bool[]>(descendingOrdinal);
            var expressions = reader.GetFieldValue<string[]>(expressionsOrdinal);
            var predicate = reader.GetStringNull(predicateOrdinal);
            var storageParameters = reader.GetFieldValueNull<string[]>(optionsOrdinal);

            var indexBuilder = new IndexBuilder()
                .WithName(indexName)
                .WithIsUnique(isUnique)
                .WithIsFiltered(predicate is not null)
                .WithFilterExpression(predicate)
                .WithIndexType(indexType);

            AddStorageParameterAnnotations(indexBuilder, storageParameters);

            for (var i = 0; i < columns.Length; i++)
            {
                var sortDirection = descendingColumns[i]
                    ? SortDirection.Descending
                    : SortDirection.Ascending;

                indexBuilder.AddColumn(columns[i], sortDirection);
            }

            if (expressions.Length > 0)
                indexBuilder.WithAnnotation(PostgreSqlAnnotations.IndexExpressions, expressions);

            var tableIndex = indexBuilder.Build();
            tables[objectId].AddIndex(tableIndex);
        }
    }

    private static async Task ReadTableTriggersAsync(
        NpgsqlConnection connection,
        Dictionary<uint, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        var objectIds = tables.Keys.ToHashSet();

        const string sql = """
            SELECT
                tr.tgrelid,
                tr.tgname,
                tr.tgenabled,
                pg_get_triggerdef(tr.oid, true),
                (tr.tgtype & 2) <> 0 AS is_before,
                (tr.tgtype & 64) <> 0 AS is_instead,
                (tr.tgtype & 4) <> 0 AS is_insert,
                (tr.tgtype & 8) <> 0 AS is_delete,
                (tr.tgtype & 16) <> 0 AS is_update
            FROM pg_trigger AS tr
            WHERE NOT tr.tgisinternal
            ORDER BY tr.tgrelid, tr.tgname
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int nameOrdinal = 1;
        const int enabledOrdinal = 2;
        const int definitionOrdinal = 3;
        const int beforeOrdinal = 4;
        const int insteadOrdinal = 5;
        const int insertOrdinal = 6;
        const int deleteOrdinal = 7;
        const int updateOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetFieldValue<uint>(objectIdOrdinal);

            if (!objectIds.Contains(objectId))
                continue; var name = reader.GetString(nameOrdinal);

            var enabled = reader.GetString(enabledOrdinal);
            var definition = reader.GetStringNull(definitionOrdinal);
            var isBefore = reader.GetBoolean(beforeOrdinal);
            var isInstead = reader.GetBoolean(insteadOrdinal);
            var isInsert = reader.GetBoolean(insertOrdinal);
            var isDelete = reader.GetBoolean(deleteOrdinal);
            var isUpdate = reader.GetBoolean(updateOrdinal);

            var timing = isInstead
                ? TriggerTiming.InsteadOf
                : isBefore ? TriggerTiming.Before : TriggerTiming.After;

            var events = TriggerEvent.None;

            if (isInsert)
                events |= TriggerEvent.Insert;

            if (isDelete)
                events |= TriggerEvent.Delete;

            if (isUpdate)
                events |= TriggerEvent.Update;

            Trigger trigger = new()
            {
                Name = name,
                Timing = timing,
                Events = events,
                Definition = definition,
                IsDisabled = enabled == "D",
            };

            tables[objectId].AddTrigger(trigger);
        }
    }
}
