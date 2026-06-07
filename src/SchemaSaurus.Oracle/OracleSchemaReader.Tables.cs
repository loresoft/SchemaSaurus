using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Oracle;

public sealed partial class OracleSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadTablesAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tables = await ReadTableDefinitionsAsync(connection, options, cancellationToken).ConfigureAwait(false);
        if (tables.Count == 0)
            return;

        await ReadRelationColumnsAsync(connection, tables, "TABLE", options, cancellationToken).ConfigureAwait(false);
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
        OracleConnection connection,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var filter = BuildObjectFilter(options, "t.OWNER", "t.TABLE_NAME");

        var sql = $"""
            SELECT
                t.OWNER,
                t.TABLE_NAME,
                tc.COMMENTS,
                t.IOT_TYPE,
                t.TEMPORARY
            FROM ALL_TABLES t
            LEFT JOIN ALL_TAB_COMMENTS tc ON tc.OWNER = t.OWNER AND tc.TABLE_NAME = t.TABLE_NAME
            WHERE {filter}
                AND t.NESTED = 'NO'
            ORDER BY t.OWNER, t.TABLE_NAME
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int nameOrdinal = 1;
        const int commentsOrdinal = 2;
        const int iotTypeOrdinal = 3;
        const int temporaryOrdinal = 4;

        var tables = new Dictionary<(string Schema, string Name), TableBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var description = reader.GetStringNull(commentsOrdinal).NullIfEmpty();
            var iotType = reader.GetStringNull(iotTypeOrdinal);
            var temporary = reader.GetStringNull(temporaryOrdinal);

            var tableBuilder = new TableBuilder()
                .WithQualifiedName(schema, name)
                .WithDescription(description)
                .WithAnnotation(OracleAnnotations.IotType, iotType)
                .WithAnnotation(OracleAnnotations.Temporary, temporary == "Y");

            tables[(schema, name)] = tableBuilder;
        }

        return tables;
    }

    private static async Task ReadTableConstraintsAsync(
        OracleConnection connection,
        Dictionary<(string Schema, string Name), TableBuilder> tables,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var filter = BuildObjectFilter(options, "c.OWNER", "c.TABLE_NAME");

        var sql = $"""
            SELECT
                c.OWNER,
                c.TABLE_NAME,
                c.CONSTRAINT_NAME,
                c.CONSTRAINT_TYPE,
                rc.OWNER AS PRINCIPAL_OWNER,
                rc.TABLE_NAME AS PRINCIPAL_TABLE,
                c.DELETE_RULE,
                cc.COLUMN_NAME,
                rcc.COLUMN_NAME AS PRINCIPAL_COLUMN,
                c.SEARCH_CONDITION_VC
            FROM ALL_CONSTRAINTS c
            LEFT JOIN ALL_CONS_COLUMNS cc
                ON cc.OWNER = c.OWNER AND cc.CONSTRAINT_NAME = c.CONSTRAINT_NAME AND cc.TABLE_NAME = c.TABLE_NAME
            LEFT JOIN ALL_CONSTRAINTS rc
                ON rc.OWNER = c.R_OWNER AND rc.CONSTRAINT_NAME = c.R_CONSTRAINT_NAME
            LEFT JOIN ALL_CONS_COLUMNS rcc
                ON rcc.OWNER = rc.OWNER AND rcc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME AND rcc.POSITION = cc.POSITION
            WHERE c.CONSTRAINT_TYPE IN ('P', 'U', 'R', 'C')
                AND {filter}
            ORDER BY c.OWNER, c.TABLE_NAME, c.CONSTRAINT_NAME, cc.POSITION
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int tableOrdinal = 1;
        const int nameOrdinal = 2;
        const int typeOrdinal = 3;
        const int principalSchemaOrdinal = 4;
        const int principalTableOrdinal = 5;
        const int deleteRuleOrdinal = 6;
        const int columnOrdinal = 7;
        const int principalColumnOrdinal = 8;
        const int checkOrdinal = 9;

        var primaryKeys = new Dictionary<(string Schema, string Table, string Name), List<ColumnReference>>();
        var uniqueConstraints = new Dictionary<(string Schema, string Table, string Name), List<ColumnReference>>();
        var foreignKeys = new Dictionary<(string Schema, string Table, string Name), ForeignKeyBuilder>();
        var checkConstraints = new Dictionary<(string Schema, string Table, string Name), string>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var tableName = reader.GetString(tableOrdinal);
            var name = reader.GetString(nameOrdinal);
            var type = reader.GetString(typeOrdinal);
            var principalSchema = reader.GetStringNull(principalSchemaOrdinal);
            var principalTable = reader.GetStringNull(principalTableOrdinal);
            var deleteRule = reader.GetStringNull(deleteRuleOrdinal);
            var columnName = reader.GetStringNull(columnOrdinal);
            var principalColumn = reader.GetStringNull(principalColumnOrdinal);
            var checkExpression = reader.GetStringNull(checkOrdinal);

            if (!tables.ContainsKey((schema, tableName)))
                continue;

            var key = (schema, tableName, name);

            if (type == "C")
            {
                if (!string.IsNullOrWhiteSpace(checkExpression) && !checkExpression.Contains("IS NOT NULL", StringComparison.OrdinalIgnoreCase))
                    checkConstraints[key] = checkExpression;

                continue;
            }

            if (columnName is null)
                continue;

            ColumnReference reference = new()
            {
                ColumnName = columnName,
            };

            if (type == "P")
            {
                if (!primaryKeys.TryGetValue(key, out var columns))
                {
                    columns = [];
                    primaryKeys[key] = columns;
                }

                columns.Add(reference);
            }
            else if (type == "U")
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
                if (principalSchema is null || principalTable is null || principalColumn is null)
                    continue;

                if (!foreignKeys.TryGetValue(key, out var foreignKeyBuilder))
                {
                    foreignKeyBuilder = new ForeignKeyBuilder()
                        .WithName(name)
                        .WithPrincipalTableName(principalSchema, principalTable)
                        .WithOnDelete(MapReferentialAction(deleteRule));

                    foreignKeys[key] = foreignKeyBuilder;
                }

                foreignKeyBuilder.AddColumnMapping(columnName, principalColumn);
            }
        }

        foreach (var ((schema, tableName, name), columns) in primaryKeys)
            tables[(schema, tableName)].WithPrimaryKey(name, false, [.. columns]);

        foreach (var ((schema, tableName, name), columns) in uniqueConstraints)
            tables[(schema, tableName)].AddUniqueConstraint(name, [.. columns]);

        foreach (var ((schema, tableName, name), expression) in checkConstraints)
            tables[(schema, tableName)].AddCheckConstraint(name, expression);

        foreach (var ((schema, tableName, _), foreignKeyBuilder) in foreignKeys)
        {
            var foreignKey = foreignKeyBuilder.Build();
            tables[(schema, tableName)].AddForeignKey(foreignKey);
        }
    }

    private static async Task ReadTableIndexesAsync(
        OracleConnection connection,
        Dictionary<(string Schema, string Name), TableBuilder> tables,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var filter = BuildObjectFilter(options, "i.TABLE_OWNER", "i.TABLE_NAME");

        var sql = $"""
            SELECT
                i.TABLE_OWNER,
                i.TABLE_NAME,
                i.INDEX_NAME,
                i.UNIQUENESS,
                i.INDEX_TYPE,
                i.STATUS,
                ic.COLUMN_NAME,
                ic.DESCEND
            FROM ALL_INDEXES i
            INNER JOIN ALL_IND_COLUMNS ic ON ic.INDEX_OWNER = i.OWNER AND ic.INDEX_NAME = i.INDEX_NAME
            WHERE {filter}
                AND NOT EXISTS (
                    SELECT 1
                    FROM ALL_CONSTRAINTS c
                    WHERE c.OWNER = i.TABLE_OWNER
                    AND c.TABLE_NAME = i.TABLE_NAME
                    AND c.INDEX_NAME = i.INDEX_NAME
                    AND c.CONSTRAINT_TYPE IN ('P', 'U')
                )
            ORDER BY i.TABLE_OWNER, i.TABLE_NAME, i.INDEX_NAME, ic.COLUMN_POSITION
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int tableOrdinal = 1;
        const int nameOrdinal = 2;
        const int uniqueOrdinal = 3;
        const int typeOrdinal = 4;
        const int statusOrdinal = 5;
        const int columnOrdinal = 6;
        const int descendOrdinal = 7;

        var indexes = new Dictionary<(string Schema, string Table, string Name), IndexBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var tableName = reader.GetString(tableOrdinal);
            var indexName = reader.GetString(nameOrdinal);
            var uniqueness = reader.GetString(uniqueOrdinal);
            var indexType = reader.GetStringNull(typeOrdinal);
            var status = reader.GetStringNull(statusOrdinal);
            var columnName = reader.GetString(columnOrdinal);
            var descend = reader.GetStringNull(descendOrdinal);

            if (!tables.ContainsKey((schema, tableName)))
                continue;

            var key = (schema, tableName, indexName);

            if (!indexes.TryGetValue(key, out var indexBuilder))
            {
                indexBuilder = new IndexBuilder()
                    .WithName(indexName)
                    .WithIsUnique(uniqueness == "UNIQUE")
                    .WithIndexType(indexType)
                    .WithIsDisabled(status == "UNUSABLE");

                indexes[key] = indexBuilder;
            }

            var sortDirection = descend == "DESC"
                ? SortDirection.Descending
                : SortDirection.Ascending;

            indexBuilder.AddColumn(columnName, sortDirection);
        }

        foreach (var ((schema, tableName, _), indexBuilder) in indexes)
        {
            var index = indexBuilder.Build();
            tables[(schema, tableName)].AddIndex(index);
        }
    }

    private static async Task ReadTableTriggersAsync(
        OracleConnection connection,
        Dictionary<(string Schema, string Name), TableBuilder> tables,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var filter = BuildObjectFilter(options, "tr.TABLE_OWNER", "tr.TABLE_NAME");

        var sql = $"""
            SELECT
                tr.TABLE_OWNER,
                tr.TABLE_NAME,
                tr.TRIGGER_NAME,
                tr.TRIGGER_TYPE,
                tr.TRIGGERING_EVENT,
                tr.STATUS,
                tr.TRIGGER_BODY
            FROM ALL_TRIGGERS tr
            WHERE {filter}
            ORDER BY tr.TABLE_OWNER, tr.TABLE_NAME, tr.TRIGGER_NAME
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int tableOrdinal = 1;
        const int nameOrdinal = 2;
        const int typeOrdinal = 3;
        const int eventOrdinal = 4;
        const int statusOrdinal = 5;
        const int bodyOrdinal = 6;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var tableName = reader.GetString(tableOrdinal);
            var name = reader.GetString(nameOrdinal);
            var triggerType = reader.GetString(typeOrdinal);
            var triggeringEvent = reader.GetString(eventOrdinal);
            var status = reader.GetString(statusOrdinal);
            var body = reader.GetStringNull(bodyOrdinal);

            if (!tables.TryGetValue((schema, tableName), out var tableBuilder))
                continue;

            Trigger trigger = new()
            {
                Name = name,
                Timing = MapTriggerTiming(triggerType),
                Events = MapTriggerEvents(triggeringEvent),
                Definition = body,
                IsDisabled = status != "ENABLED",
            };

            tableBuilder.AddTrigger(trigger);
        }
    }

    private static TriggerTiming MapTriggerTiming(string triggerType)
    {
        if (triggerType.Contains("INSTEAD OF", StringComparison.OrdinalIgnoreCase))
            return TriggerTiming.InsteadOf;

        if (triggerType.Contains("BEFORE", StringComparison.OrdinalIgnoreCase))
            return TriggerTiming.Before;

        return TriggerTiming.After;
    }

    private static TriggerEvent MapTriggerEvents(string triggeringEvent)
    {
        var events = TriggerEvent.None;

        if (triggeringEvent.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
            events |= TriggerEvent.Insert;

        if (triggeringEvent.Contains("UPDATE", StringComparison.OrdinalIgnoreCase))
            events |= TriggerEvent.Update;

        if (triggeringEvent.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
            events |= TriggerEvent.Delete;

        return events;
    }
}
