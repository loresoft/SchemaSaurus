using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

public sealed partial class SqlServerSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadTablesAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableFilter = BuildTableFilter(options);

        var tables = await ReadTableDefinitionsAsync(connection, tableFilter, cancellationToken).ConfigureAwait(false);
        if (tables.Count == 0)
            return;

        await ReadTableColumnsAsync(connection, tables, tableFilter, cancellationToken).ConfigureAwait(false);
        await ReadKeyConstraintsAsync(connection, tables, cancellationToken).ConfigureAwait(false);
        await ReadTableIndexesAsync(connection, tables, cancellationToken).ConfigureAwait(false);
        await ReadCheckConstraintsAsync(connection, tables, cancellationToken).ConfigureAwait(false);
        await ReadTableForeignKeysAsync(connection, tables, cancellationToken).ConfigureAwait(false);
        await ReadTableTriggersAsync(connection, tables, cancellationToken).ConfigureAwait(false);

        // Build the tables and add them to the DatabaseModelBuilder. We do this at the end after reading all related metadata to
        // ensure that all properties (including extended properties) are applied to the TableBuilder before it's built.
        foreach (var (_, tb) in tables)
            builder.AddTable(tb.Build());
    }

    private async Task<Dictionary<int, TableBuilder>> ReadTableDefinitionsAsync(
        SqlConnection connection,
        string tableFilter,
        CancellationToken cancellationToken)
    {
        var tables = new Dictionary<int, TableBuilder>();
        var sql = $"""
            SELECT
                t.object_id,
                SCHEMA_NAME(t.schema_id)            AS schema_name,
                t.name                              AS table_name,
                t.temporal_type,
                t.is_memory_optimized,
                t.is_filetable,
                SCHEMA_NAME(ht.schema_id)           AS history_schema,
                ht.name                             AS history_name,
                CAST(ep.value AS NVARCHAR(4000))    AS description
            FROM sys.tables t
            LEFT JOIN sys.tables ht ON t.history_table_id = ht.object_id
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = t.object_id AND ep.minor_id = 0
                AND ep.class = 1 AND ep.name = 'MS_Description'
            WHERE {tableFilter}
              AND t.temporal_type <> 1
            ORDER BY SCHEMA_NAME(t.schema_id), t.name
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int schemaOrdinal = 1;
        const int nameOrdinal = 2;
        const int temporalOrdinal = 3;
        const int memOptOrdinal = 4;
        const int fileTableOrdinal = 5;
        const int histSchemaOrdinal = 6;
        const int histNameOrdinal = 7;
        const int descOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(objectIdOrdinal);
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var temporalType = reader.GetByte(temporalOrdinal);
            var isMemOpt = reader.GetBoolean(memOptOrdinal);
            var isFileTable = reader.GetBoolean(fileTableOrdinal);
            var histSchema = reader.GetStringNull(histSchemaOrdinal);
            var histName = reader.GetStringNull(histNameOrdinal);
            var description = reader.GetStringNull(descOrdinal);

            SchemaQualifiedName? historyTableName = histSchema is not null && histName is not null
                ? new SchemaQualifiedName { Schema = histSchema, Name = histName }
                : null;

            var tableOptions = new TableOptions
            {
                IsTemporalTable = temporalType == 2,
                HistoryTableName = historyTableName,
                IsMemoryOptimized = isMemOpt,
                IsFileTable = isFileTable,
            };

            var tb = new TableBuilder()
                .WithSchemaQualifiedName(schema, name)
                .WithDescription(description)
                .WithOptions(tableOptions);

            // apply extended properties for the table itself (class=1, major_id=object_id, minor_id=0)
            ApplyExtendedProperties((1, objectId, 0), tb);

            tables[objectId] = tb;
        }

        return tables;
    }

    private async Task ReadTableColumnsAsync(
        SqlConnection connection,
        Dictionary<int, TableBuilder> tables,
        string tableFilter,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                c.object_id,
                c.column_id,
                c.name                              AS column_name,
                st.name                             AS system_type_name,
                TYPE_NAME(c.user_type_id)           AS user_type_name,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable,
                c.is_identity,
                c.is_computed,
                CAST(CASE WHEN c.system_type_id = 189 THEN 1 ELSE 0 END AS BIT) AS is_rowversion,
                c.collation_name,
                CAST(ic.seed_value AS BIGINT)       AS identity_seed,
                CAST(ic.increment_value AS BIGINT)  AS identity_increment,
                cc.definition                       AS computed_sql,
                cc.is_persisted,
                dc.definition                       AS default_sql,
                CAST(ep.value AS NVARCHAR(4000))    AS description
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            INNER JOIN sys.types st
                ON c.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            LEFT JOIN sys.identity_columns ic
                ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            LEFT JOIN sys.computed_columns cc
                ON c.object_id = cc.object_id AND c.column_id = cc.column_id
            LEFT JOIN sys.default_constraints dc
                ON c.default_object_id = dc.object_id
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
                AND ep.class = 1 AND ep.name = 'MS_Description'
            WHERE {tableFilter}
            ORDER BY c.object_id, c.column_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int columnIdOrdinal = 1;
        const int colNameOrdinal = 2;
        const int sysTypeOrdinal = 3;
        const int userTypeOrdinal = 4;
        const int maxLenOrdinal = 5;
        const int precisionOrdinal = 6;
        const int scaleOrdinal = 7;
        const int nullableOrdinal = 8;
        const int identityOrdinal = 9;
        const int computedOrdinal = 10;
        const int rowVerOrdinal = 11;
        const int collationOrdinal = 12;
        const int seedOrdinal = 13;
        const int incrOrdinal = 14;
        const int compSqlOrdinal = 15;
        const int persistedOrdinal = 16;
        const int defSqlOrdinal = 17;
        const int descOrdinal = 18;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!tables.TryGetValue(objectId, out var tableBuilder))
                continue;

            var columnId = reader.GetInt32(columnIdOrdinal);
            var columnName = reader.GetString(colNameOrdinal);
            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.GetStringNull(userTypeOrdinal) ?? systemTypeName;
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);
            var isIdentity = reader.GetBoolean(identityOrdinal);
            var isComputed = reader.GetBoolean(computedOrdinal);
            var isRowVersion = reader.GetBoolean(rowVerOrdinal);
            var collation = reader.GetStringNull(collationOrdinal);
            var identitySeed = reader.GetInt64Null(seedOrdinal);
            var identityIncrement = reader.GetInt64Null(incrOrdinal);
            var computedSql = reader.GetStringNull(compSqlOrdinal);
            var isPersisted = reader.GetBooleanNull(persistedOrdinal) ?? false;
            var defaultSql = reader.GetStringNull(defSqlOrdinal);
            var description = reader.GetStringNull(descOrdinal);

            // Map SQL Server system type to DbType and CLR type, and determine Unicode/fixed-length attributes
            var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(systemTypeName);

            // Format the native type name with length/precision/scale as appropriate for the type
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            // Normalize max length for character types (e.g. -1 for MAX) and binary types, and set to null for types where it doesn't apply
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);

            // Only set precision and scale for types where they apply (e.g. decimal, numeric, time, datetime2); set to null for other types
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;

            // Scale is applicable for decimal/numeric, and also for time/datetime2 (where it represents fractional seconds precision). For other types, set to null.
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            tableBuilder.AddColumn(columnBuilder =>
            {
                columnBuilder
                    .WithName(columnName)
                    .WithOrdinalPosition(columnId)
                    .WithIsNullable(isNullable)
                    .WithDefaultValueSql(defaultSql)
                    .WithIsIdentity(isIdentity)
                    .WithIdentitySeed(identitySeed)
                    .WithIdentityIncrement(identityIncrement)
                    .WithIsComputed(isComputed)
                    .WithComputedColumnSql(computedSql)
                    .WithIsStored(isPersisted)
                    .WithIsRowVersion(isRowVersion)
                    .WithIsConcurrencyToken(isRowVersion)
                    .WithCollation(collation)
                    .WithDescription(description)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLengthValue)
                    .WithPrecision(precisionValue)
                    .WithScale(scaleValue)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength);

                // Apply extended properties for the column (class=1, major_id=object_id, minor_id=column_id)
                ApplyExtendedProperties((1, objectId, columnId), columnBuilder);
                columnBuilder.WithAnnotation(SqlServerAnnotations.SqlDbType, sqlDbType.ToString());
            });
        }
    }

    private async Task ReadKeyConstraintsAsync(
        SqlConnection connection,
        Dictionary<int, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        // Key constraints (primary keys and unique constraints) are read together since they share underlying index metadata.

        // Get the set of object IDs for the tables we're interested in, to filter the query results efficiently.
        var objectIds = tables.Keys.ToHashSet();

        // Dictionary to accumulate constraint info, keyed by (object_id, constraint_name). Value is (constraint_type, is_clustered, list of columns in order).
        var constraints = new Dictionary<(int ObjectId, string Name), (string Type, bool IsClustered, List<ColumnReference> Columns)>();

        const string sql = """
            SELECT
                kc.parent_object_id,
                kc.name             AS constraint_name,
                kc.type             AS constraint_type,
                i.type              AS index_type,
                c.name              AS column_name,
                ic.is_descending_key
            FROM sys.key_constraints kc
            INNER JOIN sys.indexes i
                ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
            INNER JOIN sys.index_columns ic
                ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c
                ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            INNER JOIN sys.tables t ON kc.parent_object_id = t.object_id
            WHERE ic.is_included_column = 0
              AND t.is_ms_shipped = 0
            ORDER BY kc.parent_object_id, kc.name, ic.key_ordinal
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int parentOrdinal = 0;
        const int nameOrdinal = 1;
        const int typeOrdinal = 2;
        const int indexTypeOrdinal = 3;
        const int columnNameOrdinal = 4;
        const int descendOrdinal = 5;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Filter rows based on object_id to avoid processing constraints for tables we're not including.
            // This is more efficient than filtering in-memory after reading all constraints.
            var objectId = reader.GetInt32(parentOrdinal);
            if (!objectIds.Contains(objectId))
                continue;

            var constraintName = reader.GetString(nameOrdinal);
            var key = (objectId, constraintName);

            // If we haven't seen this constraint before, create a new entry in the dictionary with its type and clustering info.
            // Otherwise, we'll just add columns to the existing entry.
            if (!constraints.TryGetValue(key, out var kc))
            {
                var type = reader.GetString(typeOrdinal).Trim();
                var indexType = reader.GetByte(indexTypeOrdinal);
                kc = (type, indexType == 1, []);

                constraints[key] = kc;
            }

            var columnName = reader.GetString(columnNameOrdinal);
            var sortDirection = reader.GetBoolean(descendOrdinal) ? SortDirection.Descending : SortDirection.Ascending;

            ColumnReference reference = new()
            {
                ColumnName = columnName,
                SortDirection = sortDirection,
            };
            kc.Columns.Add(reference);
        }

        // Now that we've read all the constraints and their columns, we can apply them to the corresponding tables.
        foreach (var ((objectId, name), (type, isClustered, columns)) in constraints)
        {
            var tableBuilder = tables[objectId];

            // SQL Server represents both primary keys and unique constraints in the sys.key_constraints view,
            // distinguished by the 'type' column ('PK' for primary key, 'UQ' for unique constraint).
            // We need to check the type to determine whether to call WithPrimaryKey or AddUniqueConstraint on the TableBuilder.

            if (type == "PK")
                tableBuilder.WithPrimaryKey(name, isClustered, [.. columns]);
            else
                tableBuilder.AddUniqueConstraint(name, [.. columns]);
        }
    }

    private async Task ReadTableIndexesAsync(
        SqlConnection connection,
        Dictionary<int, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        // Get the set of object IDs for the tables we're interested in, to filter the query results efficiently.
        var objectIds = tables.Keys.ToHashSet();

        // Dictionary to accumulate index info, keyed by (object_id, index_id). Value is (object_id, IndexBuilder).
        // We need object_id in the value to associate the index with the correct table when we build it.
        var indexes = new Dictionary<(int ObjectId, int IndexId), (int ObjectId, IndexBuilder Builder)>();

        const string sql = """
            SELECT
                i.object_id,
                i.index_id,
                i.name              AS index_name,
                i.is_unique,
                i.type              AS index_type,
                i.is_disabled,
                i.has_filter,
                i.filter_definition,
                i.fill_factor,
                c.name              AS column_name,
                ic.is_descending_key,
                ic.is_included_column
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic
                ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c
                ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            WHERE t.is_ms_shipped = 0
              AND i.type > 0
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
              AND i.name IS NOT NULL
            ORDER BY i.object_id, i.index_id, ic.key_ordinal, ic.index_column_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int indexIdOrdinal = 1;
        const int nameOrdinal = 2;
        const int uniqueOrdinal = 3;
        const int typeOrdinal = 4;
        const int disabledOrdinal = 5;
        const int filterOrdinal = 6;
        const int filterDefOrdinal = 7;
        const int fillFactorOrdinal = 8;
        const int colNameOrdinal = 9;
        const int descendOrdinal = 10;
        const int includedOrdinal = 11;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(objectIdOrdinal);

            // Filter rows based on object_id to avoid processing indexes for tables we're not including.
            if (!objectIds.Contains(objectId))
                continue;

            var indexId = reader.GetInt32(indexIdOrdinal);
            var indexName = reader.GetString(nameOrdinal);
            var isUnique = reader.GetBoolean(uniqueOrdinal);
            var indexType = reader.GetByte(typeOrdinal);
            var isDisabled = reader.GetBoolean(disabledOrdinal);
            var hasFilter = reader.GetBoolean(filterOrdinal);
            var filterExpression = reader.GetStringNull(filterDefOrdinal);
            var fillFactor = reader.GetByte(fillFactorOrdinal);
            var columnName = reader.GetString(colNameOrdinal);
            var isDescending = reader.GetBoolean(descendOrdinal);
            var isIncluded = reader.GetBoolean(includedOrdinal);

            var key = (objectId, indexId);

            // If we haven't seen this index before, create a new IndexBuilder and add it to the dictionary.
            // Otherwise, we'll just add columns to the existing builder.
            if (!indexes.TryGetValue(key, out var entry))
            {
                var indexBuilder = new IndexBuilder()
                    .WithName(indexName)
                    .WithIsUnique(isUnique)
                    .WithIsClustered(indexType == 1)
                    .WithFillFactor(fillFactor == 0 ? null : fillFactor)
                    .WithIsDisabled(isDisabled);

                // Apply extended properties for the index (class=7, major_id=object_id, minor_id=index_id)
                ApplyExtendedProperties((7, objectId, indexId), indexBuilder);

                // If the index has a filter, set the IsFiltered property and the FilterExpression if it's not null.
                if (hasFilter)
                {
                    indexBuilder.WithIsFiltered(true);
                    if (filterExpression is not null)
                        indexBuilder.WithFilterExpression(filterExpression);
                }

                // SQL Server supports different index types: 1 = clustered, 2 = nonclustered, 3 = XML, 4 = spatial, 5 = columnstore, 6 = columnstore_clustered, 7 = hash.
                // We can map these to the IndexType property on the IndexBuilder.
                if (indexType is 5 or 6)
                    indexBuilder.WithIndexType("COLUMNSTORE");
                else if (indexType == 7)
                    indexBuilder.WithIndexType("HASH");

                entry = (objectId, indexBuilder);
                indexes[key] = entry;
            }

            // Included columns are part of the index but not key columns, so they don't have sort direction.
            // We need to call AddIncludedColumn instead of AddColumn for these.
            if (isIncluded)
            {
                entry.Builder.AddIncludedColumn(columnName);
            }
            else
            {
                var sortDirection = isDescending ? SortDirection.Descending : SortDirection.Ascending;
                entry.Builder.AddColumn(columnName, sortDirection);
            }
        }

        // Now that we've read all the indexes and their columns, we can apply them to the corresponding tables.
        foreach (var (_, (objectId, ib)) in indexes)
            tables[objectId].AddIndex(ib.Build());
    }

    private static async Task ReadCheckConstraintsAsync(
        SqlConnection connection,
        Dictionary<int, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        // Get the set of object IDs for the tables we're interested in, to filter the query results efficiently.
        var objectIds = tables.Keys.ToHashSet();

        const string sql = """
            SELECT
                cc.parent_object_id,
                cc.name         AS constraint_name,
                cc.definition
            FROM sys.check_constraints cc
            INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
            WHERE t.is_ms_shipped = 0
            ORDER BY cc.parent_object_id, cc.name
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int parentOrdinal = 0;
        const int nameOrdinal = 1;
        const int defOrdinal = 2;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(parentOrdinal);

            // Filter rows based on object_id to avoid processing constraints for tables we're not including.
            if (!objectIds.Contains(objectId))
                continue;

            var name = reader.GetString(nameOrdinal);
            var definition = reader.GetString(defOrdinal);

            tables[objectId].AddCheckConstraint(name, definition);
        }
    }

    private static async Task ReadTableForeignKeysAsync(
        SqlConnection connection,
        Dictionary<int, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        // Get the set of object IDs for the tables we're interested in, to filter the query results efficiently.
        var objectIds = tables.Keys.ToHashSet();

        // Dictionary to accumulate foreign key info, keyed by (parent_object_id, fk_name).
        // Value is (referenced_object_id, ForeignKeyBuilder with properties set except column mappings).
        var foreignKeys = new Dictionary<(int ObjectId, string Name), (int ObjectId, ForeignKeyBuilder Builder)>();

        const string sql = """
            SELECT
                fk.parent_object_id,
                fk.name                         AS fk_name,
                SCHEMA_NAME(rt.schema_id)       AS principal_schema,
                rt.name                         AS principal_table,
                fk.delete_referential_action,
                fk.update_referential_action,
                fk.is_disabled,
                pc.name                         AS parent_column,
                rc.name                         AS referenced_column
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc
                ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
            INNER JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
            INNER JOIN sys.columns pc
                ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
            INNER JOIN sys.columns rc
                ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
            WHERE t.is_ms_shipped = 0
            ORDER BY fk.parent_object_id, fk.name, fkc.constraint_column_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int parentOrdinal = 0;
        const int nameOrdinal = 1;
        const int pSchemaOrdinal = 2;
        const int pTableOrdinal = 3;
        const int deleteOrdinal = 4;
        const int updateOrdinal = 5;
        const int disabledOrdinal = 6;
        const int parentColOrdinal = 7;
        const int refColOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(parentOrdinal);

            // Filter rows based on parent_object_id to avoid processing foreign keys for tables we're not including.
            if (!objectIds.Contains(objectId))
                continue;

            var fkName = reader.GetString(nameOrdinal);
            var principalSchema = reader.GetString(pSchemaOrdinal);
            var principalTable = reader.GetString(pTableOrdinal);
            var onDelete = MapReferentialAction(reader.GetByte(deleteOrdinal));
            var onUpdate = MapReferentialAction(reader.GetByte(updateOrdinal));
            var isDisabled = reader.GetBoolean(disabledOrdinal);
            var parentColumn = reader.GetString(parentColOrdinal);
            var referencedColumn = reader.GetString(refColOrdinal);

            var key = (objectId, fkName);

            // If we haven't seen this foreign key before, create a new ForeignKeyBuilder and add it to the dictionary.
            if (!foreignKeys.TryGetValue(key, out var entry))
            {
                var foreignKeyBuilder = new ForeignKeyBuilder()
                    .WithName(fkName)
                    .WithPrincipalTableName(principalSchema, principalTable)
                    .WithOnDelete(onDelete)
                    .WithOnUpdate(onUpdate)
                    .WithIsDisabled(isDisabled);

                entry = (objectId, foreignKeyBuilder);
                foreignKeys[key] = entry;
            }

            // Add the column mapping to the ForeignKeyBuilder. Since the query is ordered by constraint_column_id, the columns will be added in the correct order.
            entry.Builder.AddColumnMapping(parentColumn, referencedColumn);
        }

        // Now that we've read all the foreign keys and their column mappings, we can apply them to the corresponding tables.
        foreach (var (_, (objectId, fkb)) in foreignKeys)
            tables[objectId].AddForeignKey(fkb.Build());
    }

    private static async Task ReadTableTriggersAsync(
        SqlConnection connection,
        Dictionary<int, TableBuilder> tables,
        CancellationToken cancellationToken)
    {
        // Get the set of object IDs for the tables we're interested in, to filter the query results efficiently.
        var objectIds = tables.Keys.ToHashSet();

        // Dictionary to accumulate trigger info, keyed by (parent_object_id, trigger_name).
        var triggerData = new Dictionary<(int ObjectId, string Name), (bool IsDisabled, bool IsInsteadOf, string? Definition, List<string> Events)>();

        const string sql = """
            SELECT
                tr.parent_id,
                tr.name                     AS trigger_name,
                tr.is_disabled,
                tr.is_instead_of_trigger,
                m.definition,
                te.type_desc                AS event_type
            FROM sys.triggers tr
            INNER JOIN sys.trigger_events te ON tr.object_id = te.object_id
            LEFT JOIN sys.sql_modules m ON tr.object_id = m.object_id
            WHERE tr.parent_class = 1
            ORDER BY tr.parent_id, tr.name
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int parentOrdinal = 0;
        const int nameOrdinal = 1;
        const int disabledOrdinal = 2;
        const int insteadOrdinal = 3;
        const int definitionOrdinal = 4;
        const int eventOrdinal = 5;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var parentId = reader.GetInt32(parentOrdinal);

            if (!objectIds.Contains(parentId))
                continue;

            var name = reader.GetString(nameOrdinal);
            var isDisabled = reader.GetBoolean(disabledOrdinal);
            var isInsteadOf = reader.GetBoolean(insteadOrdinal);
            var definition = reader.GetStringNull(definitionOrdinal);
            var eventType = reader.GetString(eventOrdinal);

            var key = (parentId, name);

            // If we haven't seen this trigger before, create a new entry in the dictionary with its disabled/instead_of/definition info.
            if (!triggerData.TryGetValue(key, out var td))
            {
                td = (isDisabled, isInsteadOf, definition, []);
                triggerData[key] = td;
            }

            td.Events.Add(eventType);
        }

        // Now that we've read all the triggers and their events, we can apply them to the corresponding tables.
        foreach (var ((parentId, name), (isDisabled, isInsteadOf, definition, events)) in triggerData)
        {
            // SQL Server table triggers expose DML events here; ignore any event types not represented by TriggerEvent.
            var triggerEvents = TriggerEvent.None;
            foreach (var evt in events)
            {
                triggerEvents |= evt switch
                {
                    "INSERT" => TriggerEvent.Insert,
                    "UPDATE" => TriggerEvent.Update,
                    "DELETE" => TriggerEvent.Delete,
                    _ => TriggerEvent.None,
                };
            }

            // Determine the trigger timing based on the is_instead_of_trigger column.
            // If it's an INSTEAD OF trigger, the timing is InsteadOf; otherwise, it's After (SQL Server doesn't have BEFORE triggers).
            var timing = isInsteadOf ? TriggerTiming.InsteadOf : TriggerTiming.After;
            var trigger = new Trigger
            {
                Name = name,
                Timing = timing,
                Events = triggerEvents,
                Definition = definition,
                IsDisabled = isDisabled,
            };

            tables[parentId].AddTrigger(trigger);
        }
    }
}
