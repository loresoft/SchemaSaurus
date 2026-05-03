using System.Data;
using System.Data.Common;
using System.Globalization;

using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

/// <summary>
/// Reads structural metadata from a SQL Server database using <c>sys.*</c> catalog views.
/// Schema/table filtering is pushed into SQL WHERE clauses. Extended properties (MS_Description)
/// are joined inline. Large read methods are decomposed into focused private sub-methods.
/// </summary>
public sealed class SqlServerSchemaReader : DatabaseSchemaReader<SqlConnection>
{
    /// <inheritdoc />
    public override string ProviderName => "SqlServer";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CAST(SERVERPROPERTY('Collation') AS NVARCHAR(256))  AS collation,
                SCHEMA_NAME()                                       AS default_schema,
                @@VERSION                                           AS server_version,
                CAST(SERVERPROPERTY('Edition') AS NVARCHAR(256))    AS edition,
                CAST(SERVERPROPERTY('EngineEdition') AS INT)        AS engine_edition,
                (SELECT compatibility_level
                 FROM sys.databases
                 WHERE name = DB_NAME())                            AS compat_level
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        const int collationOrdinal = 0;
        const int schemaOrdinal = 1;
        const int versionOrdinal = 2;
        const int editionOrdinal = 3;
        const int engineOrdinal = 4;
        const int compatOrdinal = 5;

        var collation = reader.IsDBNull(collationOrdinal) ? null : reader.GetString(collationOrdinal);
        var defaultSchema = reader.IsDBNull(schemaOrdinal) ? null : reader.GetString(schemaOrdinal);
        var serverVersion = reader.IsDBNull(versionOrdinal) ? null : reader.GetString(versionOrdinal);
        var edition = reader.IsDBNull(editionOrdinal) ? null : reader.GetString(editionOrdinal);
        var compatibilityLevel = reader.IsDBNull(compatOrdinal) ? null : reader.GetByte(compatOrdinal).ToString(CultureInfo.InvariantCulture);

        builder
            .WithCollation(collation)
            .WithDefaultSchemaName(defaultSchema)
            .WithServerVersion(serverVersion)
            .WithEdition(edition)
            .WithCompatibilityLevel(compatibilityLevel);

        var engineEdition = reader.IsDBNull(engineOrdinal) ? 0 : reader.GetInt32(engineOrdinal);
        var engineEditionName = engineEdition switch
        {
            1 => "Personal",
            2 => "Standard",
            3 => "Enterprise",
            4 => "Express",
            5 => "AzureSQLDatabase",
            6 => "AzureSQLManagedInstance",
            _ => "Unknown"
        };

        builder.WithAnnotation("EngineEdition", engineEditionName);
    }

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

        foreach (var (_, tb) in tables)
            builder.AddTable(tb.Build());
    }

    private static async Task<Dictionary<int, TableBuilder>> ReadTableDefinitionsAsync(
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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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
            var histSchema = reader.IsDBNull(histSchemaOrdinal) ? null : reader.GetString(histSchemaOrdinal);
            var histName = reader.IsDBNull(histNameOrdinal) ? null : reader.GetString(histNameOrdinal);
            var description = reader.IsDBNull(descOrdinal) ? null : reader.GetString(descOrdinal);

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

            tables[objectId] = tb;
        }

        return tables;
    }

    private static async Task ReadTableColumnsAsync(
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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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
            if (!tables.TryGetValue(objectId, out var tb))
                continue;

            var columnId = reader.GetInt32(columnIdOrdinal);
            var columnName = reader.GetString(colNameOrdinal);
            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.IsDBNull(userTypeOrdinal) ? systemTypeName : reader.GetString(userTypeOrdinal);
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);
            var isIdentity = reader.GetBoolean(identityOrdinal);
            var isComputed = reader.GetBoolean(computedOrdinal);
            var isRowVersion = reader.GetBoolean(rowVerOrdinal);
            var collation = reader.IsDBNull(collationOrdinal) ? null : reader.GetString(collationOrdinal);
            var identitySeed = reader.IsDBNull(seedOrdinal) ? (long?)null : reader.GetInt64(seedOrdinal);
            var identityIncrement = reader.IsDBNull(incrOrdinal) ? (long?)null : reader.GetInt64(incrOrdinal);
            var computedSql = reader.IsDBNull(compSqlOrdinal) ? null : reader.GetString(compSqlOrdinal);
            var isPersisted = !reader.IsDBNull(persistedOrdinal) && reader.GetBoolean(persistedOrdinal);
            var defaultSql = reader.IsDBNull(defSqlOrdinal) ? null : reader.GetString(defSqlOrdinal);
            var description = reader.IsDBNull(descOrdinal) ? null : reader.GetString(descOrdinal);

            // Map SQL Server system type to DbType and CLR type, and determine Unicode/fixed-length attributes
            var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);

            // Format the native type name with length/precision/scale as appropriate for the type
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            // Normalize max length for character types (e.g. -1 for MAX) and binary types, and set to null for types where it doesn't apply
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);

            // Only set precision and scale for types where they apply (e.g. decimal, numeric, time, datetime2); set to null for other types
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;

            // Scale is applicable for decimal/numeric, and also for time/datetime2 (where it represents fractional seconds precision). For other types, set to null.
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            tb.AddColumn(col => col
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
                .WithIsFixedLength(isFixedLength));
        }
    }

    private static async Task ReadKeyConstraintsAsync(
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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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
            var tb = tables[objectId];

            // SQL Server represents both primary keys and unique constraints in the sys.key_constraints view,
            // distinguished by the 'type' column ('PK' for primary key, 'UQ' for unique constraint).
            // We need to check the type to determine whether to call WithPrimaryKey or AddUniqueConstraint on the TableBuilder.

            if (type == "PK")
                tb.WithPrimaryKey(name, isClustered, [.. columns]);
            else
                tb.AddUniqueConstraint(name, [.. columns]);
        }
    }

    private static async Task ReadTableIndexesAsync(
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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int indexIdOrdinal = 1;
        const int nameOrdinal = 2;
        const int uniqueOrdinal = 3;
        const int typeOrdinal = 4;
        const int disabledOrdinal = 5;
        const int filterOrdinal = 6;
        const int filterDefOrdinal = 7;
        const int colNameOrdinal = 8;
        const int descendOrdinal = 9;
        const int includedOrdinal = 10;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Filter rows based on object_id to avoid processing indexes for tables we're not including.
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!objectIds.Contains(objectId))
                continue;

            var indexId = reader.GetInt32(indexIdOrdinal);
            var key = (objectId, indexId);

            // If we haven't seen this index before, create a new IndexBuilder and add it to the dictionary.
            // Otherwise, we'll just add columns to the existing builder.
            if (!indexes.TryGetValue(key, out var entry))
            {
                var indexType = reader.GetByte(typeOrdinal);
                var indexName = reader.GetString(nameOrdinal);
                var isUnique = reader.GetBoolean(uniqueOrdinal);
                var isDisabled = reader.GetBoolean(disabledOrdinal);

                var ib = new IndexBuilder()
                    .WithName(indexName)
                    .WithIsUnique(isUnique)
                    .WithIsClustered(indexType == 1)
                    .WithIsDisabled(isDisabled);

                // If the index has a filter, set the IsFiltered property and the FilterExpression if it's not null.
                var hasFilter = reader.GetBoolean(filterOrdinal);
                if (hasFilter)
                {
                    ib.WithIsFiltered(true);
                    if (!reader.IsDBNull(filterDefOrdinal))
                    {
                        var filterExpression = reader.GetString(filterDefOrdinal);
                        ib.WithFilterExpression(filterExpression);
                    }
                }

                // SQL Server supports different index types: 1 = clustered, 2 = nonclustered, 3 = XML, 4 = spatial, 5 = columnstore, 6 = columnstore_clustered, 7 = hash.
                // We can map these to the IndexType property on the IndexBuilder.
                if (indexType is 5 or 6)
                    ib.WithIndexType("COLUMNSTORE");
                else if (indexType == 7)
                    ib.WithIndexType("HASH");

                entry = (objectId, ib);
                indexes[key] = entry;
            }

            var columnName = reader.GetString(colNameOrdinal);
            var isDescending = reader.GetBoolean(descendOrdinal);
            var isIncluded = reader.GetBoolean(includedOrdinal);

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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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
            // Filter rows based on parent_object_id to avoid processing foreign keys for tables we're not including.
            var objectId = reader.GetInt32(parentOrdinal);
            if (!objectIds.Contains(objectId))
                continue;

            var fkName = reader.GetString(nameOrdinal);
            var key = (objectId, fkName);

            // If we haven't seen this foreign key before, create a new ForeignKeyBuilder and add it to the dictionary.
            if (!foreignKeys.TryGetValue(key, out var entry))
            {
                var principalSchema = reader.GetString(pSchemaOrdinal);
                var principalTable = reader.GetString(pTableOrdinal);
                var onDelete = MapReferentialAction(reader.GetByte(deleteOrdinal));
                var onUpdate = MapReferentialAction(reader.GetByte(updateOrdinal));
                var isDisabled = reader.GetBoolean(disabledOrdinal);

                var fkb = new ForeignKeyBuilder()
                    .WithName(fkName)
                    .WithPrincipalTableName(principalSchema, principalTable)
                    .WithOnDelete(onDelete)
                    .WithOnUpdate(onUpdate)
                    .WithIsDisabled(isDisabled);

                entry = (objectId, fkb);
                foreignKeys[key] = entry;
            }

            var parentColumn = reader.GetString(parentColOrdinal);
            var referencedColumn = reader.GetString(refColOrdinal);

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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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
            var key = (parentId, name);

            // If we haven't seen this trigger before, create a new entry in the dictionary with its disabled/instead_of/definition info.
            if (!triggerData.TryGetValue(key, out var td))
            {
                var isDisabled = reader.GetBoolean(disabledOrdinal);
                var isInsteadOf = reader.GetBoolean(insteadOrdinal);
                var definition = reader.IsDBNull(definitionOrdinal) ? null : reader.GetString(definitionOrdinal);

                td = (isDisabled, isInsteadOf, definition, []);
                triggerData[key] = td;
            }

            var eventType = reader.GetString(eventOrdinal);
            td.Events.Add(eventType);
        }

        // Now that we've read all the triggers and their events, we can apply them to the corresponding tables.
        foreach (var ((parentId, name), (isDisabled, isInsteadOf, definition, events)) in triggerData)
        {
            // Map the list of event type descriptions to the TriggerEvent flags enum. The event type descriptions can be "INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "EXECUTE".
            // We only care about the first three since those are the ones that can be specified in a CREATE TRIGGER statement for DDL triggers.
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

    /// <inheritdoc />
    protected override async Task ReadViewsAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Build the WHERE clause for filtering views based on the specified schemas.
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(v.schema_id)");

        // We also filter out system views (is_ms_shipped = 0).
        var whereClause = schemaFilter is not null
            ? $"v.is_ms_shipped = 0\n    AND {schemaFilter}"
            : "v.is_ms_shipped = 0";

        var views = await ReadViewDefinitionsAsync(connection, whereClause, cancellationToken)
            .ConfigureAwait(false);

        if (views.Count == 0)
            return;

        await ReadViewColumnsAsync(connection, views, whereClause, cancellationToken).ConfigureAwait(false);

        foreach (var (_, vb) in views)
            builder.AddView(vb.Build());
    }

    private static async Task<Dictionary<int, ViewBuilder>> ReadViewDefinitionsAsync(
        SqlConnection connection,
        string whereClause,
        CancellationToken cancellationToken)
    {
        // Dictionary to hold view builders keyed by object_id, so we can populate columns in a second pass.
        var views = new Dictionary<int, ViewBuilder>();

        var sql = $"""
            SELECT
                v.object_id,
                SCHEMA_NAME(v.schema_id)            AS schema_name,
                v.name                              AS view_name,
                m.definition,
                CAST(ep.value AS NVARCHAR(4000))    AS description,
                CASE WHEN EXISTS (
                    SELECT 1 FROM sys.indexes i
                    WHERE i.object_id = v.object_id AND i.type = 1
                ) THEN 1 ELSE 0 END                AS is_materialized
            FROM sys.views v
            LEFT JOIN sys.sql_modules m ON v.object_id = m.object_id
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = v.object_id AND ep.minor_id = 0
                AND ep.class = 1 AND ep.name = 'MS_Description'
            WHERE {whereClause}
            ORDER BY SCHEMA_NAME(v.schema_id), v.name
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int schemaOrdinal = 1;
        const int nameOrdinal = 2;
        const int defOrdinal = 3;
        const int descOrdinal = 4;
        const int materializedOrdinal = 5;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(objectIdOrdinal);
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var definition = reader.IsDBNull(defOrdinal) ? null : reader.GetString(defOrdinal);
            var description = reader.IsDBNull(descOrdinal) ? null : reader.GetString(descOrdinal);
            var isMaterialized = reader.GetInt32(materializedOrdinal) == 1;

            var vb = new ViewBuilder()
                .WithSchemaQualifiedName(schema, name)
                .WithDefinition(definition)
                .WithDescription(description)
                .WithIsMaterialized(isMaterialized);

            views[objectId] = vb;
        }

        return views;
    }

    private static async Task ReadViewColumnsAsync(
        SqlConnection connection,
        Dictionary<int, ViewBuilder> views,
        string whereClause,
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
                c.collation_name,
                CAST(ep.value AS NVARCHAR(4000))    AS description
            FROM sys.columns c
            INNER JOIN sys.views v ON c.object_id = v.object_id
            INNER JOIN sys.types st
                ON c.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
                AND ep.class = 1 AND ep.name = 'MS_Description'
            WHERE {whereClause}
            ORDER BY c.object_id, c.column_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int columnIdOrdinal = 1;
        const int colNameOrdinal = 2;
        const int sysTypeOrdinal = 3;
        const int userTypeOrdinal = 4;
        const int maxLenOrdinal = 5;
        const int precisionOrdinal = 6;
        const int scaleOrdinal = 7;
        const int nullableOrdinal = 8;
        const int collationOrdinal = 9;
        const int descOrdinal = 10;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Filter rows based on object_id to avoid processing columns for views we're not including.
            // This is more efficient than filtering in-memory after reading all columns.
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!views.TryGetValue(objectId, out var vb))
                continue;

            var columnId = reader.GetInt32(columnIdOrdinal);
            var columnName = reader.GetString(colNameOrdinal);
            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.IsDBNull(userTypeOrdinal) ? systemTypeName : reader.GetString(userTypeOrdinal);
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);
            var collation = reader.IsDBNull(collationOrdinal) ? null : reader.GetString(collationOrdinal);
            var description = reader.IsDBNull(descOrdinal) ? null : reader.GetString(descOrdinal);

            // Map SQL Server system type to DbType and CLR type, and determine Unicode/fixed-length attributes
            var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);

            // Format the native type name with length/precision/scale as appropriate for the type. For user-defined types, include the user type name instead of the system type name.
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            // Normalize max length for character types (e.g. -1 for MAX) and binary types, and set to null for types where it doesn't apply
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);

            // Only set precision and scale for types where they apply (e.g. decimal, numeric, time, datetime2); set to null for other types
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;

            // Scale is applicable for decimal/numeric, and also for time/datetime2 (where it represents fractional seconds precision). For other types, set to null.
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            vb.AddColumn(col => col
                .WithName(columnName)
                .WithOrdinalPosition(columnId)
                .WithIsNullable(isNullable)
                .WithCollation(collation)
                .WithDescription(description)
                .WithNativeTypeName(nativeTypeName)
                .WithDbType(dbType)
                .WithSystemType(systemType)
                .WithMaxLength(maxLengthValue)
                .WithPrecision(precisionValue)
                .WithScale(scaleValue)
                .WithIsUnicode(isUnicode)
                .WithIsFixedLength(isFixedLength));
        }
    }

    /// <inheritdoc />
    protected override async Task ReadSequencesAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Build the WHERE clause for filtering sequences based on the specified schemas.
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(s.schema_id)");

        // We don't need to filter out system objects here since sequences are not included in the metadata for system objects (is_ms_shipped is not a column in sys.sequences).
        var whereClause = schemaFilter is not null ? $"WHERE {schemaFilter}" : "";

        var sql = $"""
            SELECT
                s.name                          AS seq_name,
                SCHEMA_NAME(s.schema_id)        AS schema_name,
                TYPE_NAME(s.system_type_id)     AS type_name,
                CAST(s.start_value AS BIGINT)   AS start_value,
                CAST(s.increment AS BIGINT)     AS increment,
                CAST(s.minimum_value AS BIGINT) AS minimum_value,
                CAST(s.maximum_value AS BIGINT) AS maximum_value,
                s.is_cycling,
                s.cache_size,
                s.is_cached
            FROM sys.sequences s
            {whereClause}
            ORDER BY SCHEMA_NAME(s.schema_id), s.name
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        const int nameOrdinal = 0;
        const int schemaOrdinal = 1;
        const int typeOrdinal = 2;
        const int startOrdinal = 3;
        const int incrOrdinal = 4;
        const int minOrdinal = 5;
        const int maxOrdinal = 6;
        const int cycleOrdinal = 7;
        const int cacheOrdinal = 8;
        const int cachedOrdinal = 9;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var seqName = reader.GetString(nameOrdinal);
            var schema = reader.GetString(schemaOrdinal);
            var typeName = reader.GetString(typeOrdinal);
            var startValue = reader.GetInt64(startOrdinal);
            var increment = reader.GetInt64(incrOrdinal);
            var minValue = reader.GetInt64(minOrdinal);
            var maxValue = reader.GetInt64(maxOrdinal);
            var isCycling = reader.GetBoolean(cycleOrdinal);

            int? cacheSize = reader.GetBoolean(cachedOrdinal)
                ? (reader.IsDBNull(cacheOrdinal) ? null : reader.GetInt32(cacheOrdinal))
                : null;

            // Map SQL Server system type to DbType and CLR type
            var (dbType, systemType, _, _) = MapSqlServerType(typeName);

            builder.AddSequence(seq => seq
                .WithSchemaQualifiedName(schema, seqName)
                .WithDbType(dbType)
                .WithSystemType(systemType)
                .WithStartValue(startValue)
                .WithIncrement(increment)
                .WithMinValue(minValue)
                .WithMaxValue(maxValue)
                .WithIsCycling(isCycling)
                .WithCacheSize(cacheSize));
        }
    }

    /// <inheritdoc />
    protected override async Task ReadStoredProceduresAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Build the WHERE clause for filtering stored procedures based on the specified schemas, and also filter out system objects (is_ms_shipped = 0).
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(p.schema_id)");

        // If a schema filter was built, include it in the WHERE clause; otherwise, just filter out system objects.
        var schemaWhere = schemaFilter is not null ? $"\n    AND {schemaFilter}" : "";

        // Dictionary to hold stored procedure builders keyed by object_id, so we can populate parameters in a second pass.
        var procs = new Dictionary<int, StoredProcedureBuilder>();

        var sql = $"""
            SELECT
                p.object_id,
                SCHEMA_NAME(p.schema_id)            AS schema_name,
                p.name                              AS proc_name,
                m.definition,
                CAST(ep.value AS NVARCHAR(4000))    AS description
            FROM sys.procedures p
            LEFT JOIN sys.sql_modules m ON p.object_id = m.object_id
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = p.object_id AND ep.minor_id = 0
                AND ep.class = 1 AND ep.name = 'MS_Description'
            WHERE p.is_ms_shipped = 0{schemaWhere}
            ORDER BY SCHEMA_NAME(p.schema_id), p.name
            """;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            const int objectIdOrdinal = 0;
            const int schemaOrdinal = 1;
            const int nameOrdinal = 2;
            const int definitionOrdinal = 3;
            const int descriptionOrdinal = 4;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(objectIdOrdinal);
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var definition = reader.IsDBNull(definitionOrdinal) ? null : reader.GetString(definitionOrdinal);
                var description = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal);

                var spb = new StoredProcedureBuilder()
                    .WithSchemaQualifiedName(schema, name)
                    .WithDefinition(definition)
                    .WithDescription(description);

                procs[objectId] = spb;
            }
        }

        if (procs.Count == 0)
            return;

        await ReadParametersAsync(connection, procs, "sys.procedures", cancellationToken).ConfigureAwait(false);

        // Now that we've read all the stored procedures and their parameters, we can build them and add them to the builder.
        foreach (var (_, spb) in procs)
            builder.AddStoredProcedure(spb.Build());
    }

    /// <inheritdoc />
    protected override async Task ReadScalarFunctionsAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Build the WHERE clause for filtering scalar functions based on the specified schemas, and also filter out system objects (is_ms_shipped = 0).
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(o.schema_id)");

        // If a schema filter was built, include it in the WHERE clause; otherwise, just filter out system objects.
        var schemaWhere = schemaFilter is not null ? $"\n    AND {schemaFilter}" : "";

        // Dictionary to hold scalar function builders keyed by object_id, so we can populate parameters in a second pass.
        // We use the same ReadParametersAsync method as for stored procedures since the parameter metadata is in the same format,
        // but we need separate builders since stored procedures and functions have different metadata properties (e.g. return type for functions).
        var funcs = new Dictionary<int, ScalarFunctionBuilder>();

        var sql = $"""
            SELECT
                o.object_id,
                SCHEMA_NAME(o.schema_id)    AS schema_name,
                o.name                      AS func_name,
                m.definition
            FROM sys.objects o
            LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
            WHERE o.type = 'FN' AND o.is_ms_shipped = 0{schemaWhere}
            ORDER BY SCHEMA_NAME(o.schema_id), o.name
            """;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            const int objectIdOrdinal = 0;
            const int schemaOrdinal = 1;
            const int nameOrdinal = 2;
            const int definitionOrdinal = 3;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(objectIdOrdinal);
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var definition = reader.IsDBNull(definitionOrdinal) ? null : reader.GetString(definitionOrdinal);

                var fb = new ScalarFunctionBuilder()
                    .WithSchemaQualifiedName(schema, name)
                    .WithDefinition(definition);

                funcs[objectId] = fb;
            }
        }

        if (funcs.Count == 0)
            return;

        await ReadScalarFunctionParametersAsync(connection, funcs, cancellationToken).ConfigureAwait(false);

        // Now that we've read all the scalar functions and their parameters, we can build them and add them to the builder.
        foreach (var (_, fb) in funcs)
            builder.AddScalarFunction(fb.Build());
    }

    private static async Task ReadScalarFunctionParametersAsync(
        SqlConnection connection,
        Dictionary<int, ScalarFunctionBuilder> funcs,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                par.object_id,
                par.name                        AS param_name,
                par.parameter_id,
                st.name                         AS system_type_name,
                TYPE_NAME(par.user_type_id)     AS user_type_name,
                par.max_length,
                par.precision,
                par.scale,
                par.is_output
            FROM sys.parameters par
            INNER JOIN sys.objects o ON par.object_id = o.object_id
            INNER JOIN sys.types st
                ON par.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            WHERE o.type = 'FN' AND o.is_ms_shipped = 0
            ORDER BY par.object_id, par.parameter_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int nameOrdinal = 1;
        const int paramIdOrdinal = 2;
        const int sysTypeOrdinal = 3;
        const int userTypeOrdinal = 4;
        const int maxLenOrdinal = 5;
        const int precisionOrdinal = 6;
        const int scaleOrdinal = 7;
        const int outputOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Filter rows based on object_id to avoid processing parameters for functions we're not including.
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!funcs.TryGetValue(objectId, out var fb))
                continue;

            var paramId = reader.GetInt32(paramIdOrdinal);
            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.IsDBNull(userTypeOrdinal) ? systemTypeName : reader.GetString(userTypeOrdinal);
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);

            // Normalize max length for character types (e.g. -1 for MAX) and binary types, and set to null for types where it doesn't apply
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);

            // Only set precision for types where it applies (e.g. decimal, numeric, time, datetime2); set to null for other types
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;

            // Scale is applicable for decimal/numeric, and also for time/datetime2 (where it represents fractional seconds precision). For other types, set to null.
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            // Map SQL Server system type to DbType and CLR type, and determine Unicode/fixed-length attributes
            var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);

            // Format the native type name with length/precision/scale as appropriate for the type. For user-defined types, include the user type name instead of the system type name.
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            // If parameter_id = 0, this row describes the return type of the function; otherwise, it describes a regular parameter.
            // In either case, we use the same metadata to build a TypeMapping for the return type or parameter type.
            if (paramId == 0)
            {
                fb.WithReturnType(new TypeMapping
                {
                    DbType = dbType,
                    NativeTypeName = nativeTypeName,
                    SystemType = systemType,
                    MaxLength = maxLengthValue,
                    Precision = precisionValue,
                    Scale = scaleValue,
                    IsUnicode = isUnicode,
                    IsFixedLength = isFixedLength,
                });
            }
            else
            {
                var paramName = reader.GetString(nameOrdinal);
                var direction = reader.GetBoolean(outputOrdinal) ? Metadata.ParameterDirection.Output : Metadata.ParameterDirection.Input;

                fb.AddParameter(p => p
                    .WithName(paramName)
                    .WithOrdinal(paramId)
                    .WithDirection(direction)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLengthValue)
                    .WithPrecision(precisionValue)
                    .WithScale(scaleValue)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength));
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ReadTableValuedFunctionsAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Build the WHERE clause for filtering table-valued functions based on the specified schemas, and also filter out system objects (is_ms_shipped = 0).
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(o.schema_id)");

        // If a schema filter was built, include it in the WHERE clause; otherwise, just filter out system objects.
        var schemaWhere = schemaFilter is not null ? $"\n    AND {schemaFilter}" : "";

        // Dictionary to hold table-valued function builders keyed by object_id, so we can populate parameters and return columns in subsequent passes.
        var funcs = new Dictionary<int, TableValuedFunctionBuilder>();

        var sql = $"""
            SELECT
                o.object_id,
                SCHEMA_NAME(o.schema_id)    AS schema_name,
                o.name                      AS func_name,
                m.definition
            FROM sys.objects o
            LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
            WHERE o.type IN ('TF', 'IF') AND o.is_ms_shipped = 0{schemaWhere}
            ORDER BY SCHEMA_NAME(o.schema_id), o.name
            """;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            const int objectIdOrdinal = 0;
            const int schemaOrdinal = 1;
            const int nameOrdinal = 2;
            const int defOrdinal = 3;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(objectIdOrdinal);
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var definition = reader.IsDBNull(defOrdinal) ? null : reader.GetString(defOrdinal);

                var fb = new TableValuedFunctionBuilder()
                    .WithSchemaQualifiedName(schema, name)
                    .WithDefinition(definition);

                funcs[objectId] = fb;
            }
        }

        if (funcs.Count == 0)
            return;

        // Table-valued functions can have both parameters and return columns, so we need to read the parameters first since the return columns metadata doesn't
        // include parameter information (e.g. for inline table-valued functions, the return columns can depend on the parameters).
        await ReadParametersAsync(connection, funcs, "sys.objects o2", "o.type IN ('TF', 'IF')", cancellationToken).ConfigureAwait(false);

        await ReadTableValuedFunctionColumnsAsync(connection, funcs, cancellationToken).ConfigureAwait(false);

        // Now that we've read all the table-valued functions, their parameters, and their return columns, we can build them and add them to the builder.
        foreach (var (_, fb) in funcs)
            builder.AddTableValuedFunction(fb.Build());
    }

    private static async Task ReadTableValuedFunctionColumnsAsync(
        SqlConnection connection,
        Dictionary<int, TableValuedFunctionBuilder> funcs,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.object_id,
                c.column_id,
                c.name                      AS column_name,
                st.name                     AS system_type_name,
                TYPE_NAME(c.user_type_id)   AS user_type_name,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable
            FROM sys.columns c
            INNER JOIN sys.objects o ON c.object_id = o.object_id
            INNER JOIN sys.types st
                ON c.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            WHERE o.type IN ('TF', 'IF') AND o.is_ms_shipped = 0
            ORDER BY c.object_id, c.column_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int columnIdOrdinal = 1;
        const int columnNameOrdinal = 2;
        const int sysTypeOrdinal = 3;
        const int userTypeOrdinal = 4;
        const int maxLenOrdinal = 5;
        const int precisionOrdinal = 6;
        const int scaleOrdinal = 7;
        const int nullableOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Filter rows based on object_id to avoid processing columns for functions we're not including.
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!funcs.TryGetValue(objectId, out var fb))
                continue;

            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.IsDBNull(userTypeOrdinal) ? systemTypeName : reader.GetString(userTypeOrdinal);
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var columnName = reader.GetString(columnNameOrdinal);
            var columnId = reader.GetInt32(columnIdOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);

            // map SQL Server system type to DbType and CLR type, and determine Unicode/fixed-length attributes
            var (dbType, systemType, _, _) = MapSqlServerType(systemTypeName);

            // Format the native type name with length/precision/scale as appropriate for the type. For user-defined types, include the user type name instead of the system type name.
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            fb.AddReturnColumn(
                columnName,
                columnId,
                dbType,
                nativeTypeName,
                systemType,
                isNullable);
        }
    }

    /// <inheritdoc />
    protected override async Task ReadUserDefinedTypesAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Build the WHERE clause for filtering user-defined types based on the specified schemas, and also filter out system objects (is_ms_shipped = 0).
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(t.schema_id)");

        // If a schema filter was built, include it in the WHERE clause; otherwise, just filter out system objects. Note that for user-defined types,
        // we need to include both user-defined types and table types, so we can't filter by type here; instead, we'll filter in-memory after reading all types.
        var schemaWhere = schemaFilter is not null ? $"\n    AND {schemaFilter}" : "";

        // Dictionary to hold user-defined type builders keyed by user_type_id, so we can populate table type columns in a second pass for table types.
        var udts = new Dictionary<int, UserDefinedTypeBuilder>();

        // Dictionary to map type_table_object_id to user_type_id for table types, so we can link the table type columns to the correct user-defined type builder in the second pass
        // (note that not all user-defined types are table types, but all table types are user-defined types, so we can use user_type_id as the key in the udts dictionary).
        var tableTypeObjectIds = new Dictionary<int, int>(); // type_table_object_id → user_type_id

        var sql = $"""
            SELECT
                t.user_type_id,
                SCHEMA_NAME(t.schema_id)    AS schema_name,
                t.name                      AS type_name,
                t.is_table_type,
                st.name                     AS base_type_name,
                t.max_length,
                t.precision,
                t.scale,
                t.is_nullable,
                tt.type_table_object_id
            FROM sys.types t
            LEFT JOIN sys.types st
                ON t.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            LEFT JOIN sys.table_types tt ON t.user_type_id = tt.user_type_id
            WHERE (t.is_user_defined = 1 OR t.is_table_type = 1){schemaWhere}
            ORDER BY SCHEMA_NAME(t.schema_id), t.name
            """;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            const int userTypeIdOrdinal = 0;
            const int schemaOrdinal = 1;
            const int nameOrdinal = 2;
            const int isTableOrdinal = 3;
            const int baseTypeOrdinal = 4;
            const int maxLenOrdinal = 5;
            const int precisionOrdinal = 6;
            const int scaleOrdinal = 7;
            const int ttObjOrdinal = 9;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var userTypeId = reader.GetInt32(userTypeIdOrdinal);
                var schema = reader.GetString(schemaOrdinal);
                var typeName = reader.GetString(nameOrdinal);
                var isTableType = reader.GetBoolean(isTableOrdinal);
                var baseTypeName = reader.IsDBNull(baseTypeOrdinal) ? "table" : reader.GetString(baseTypeOrdinal);
                var maxLength = reader.GetInt16(maxLenOrdinal);
                var precision = reader.GetByte(precisionOrdinal);
                var scale = reader.GetByte(scaleOrdinal);

                // Map SQL Server system type to DbType and CLR type, and determine Unicode/fixed-length attributes for the base type of the user-defined type. For table types, the base type is always "table",
                // which we handle as a special case in the MapSqlServerType method to return appropriate metadata (e.g. DbType = Object, SystemType = typeof(object), etc.).
                var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(baseTypeName);

                // For table types, we set the kind to TableType and the native type name to "table", and we don't set max length, precision, scale, Unicode, or fixed-length attributes since they don't apply to table types.
                // For regular user-defined types, we set the kind to Alias and format the native type name based on the base type and its attributes.
                var kind = isTableType ? UserDefinedTypeKind.TableType : UserDefinedTypeKind.Alias;

                // Format the native type name with length/precision/scale as appropriate for the base type. For table types, we just use "table" as the native type name.
                var nativeTypeName = isTableType ? "table" : FormatNativeTypeName(baseTypeName, baseTypeName, maxLength, precision, scale);

                // For table types, we use typeof(object) as the CLR type since they don't have a specific CLR type; for regular user-defined types, we use the CLR type of the base type.
                var systemTypeValue = isTableType ? typeof(object) : systemType;

                // For table types, max length, precision, scale, Unicode, and fixed-length attributes don't apply, so we set them to null;
                // for regular user-defined types, we set them based on the base type.
                var maxLengthValue = isTableType ? null : NormalizeMaxLength(baseTypeName, maxLength);

                // Only set precision for types where it applies (e.g. decimal, numeric, time, datetime2); set to null for other types and for table types
                byte? precisionValue = HasPrecision(baseTypeName) ? precision : null;

                // Scale is applicable for decimal/numeric, and also for time/datetime2 (where it represents fractional seconds precision).
                // For other types and for table types, set to null.
                var scaleValue = HasScale(baseTypeName) ? (int?)scale : null;

                var unicode = isTableType ? null : isUnicode;
                var fixedLength = isTableType ? null : isFixedLength;

                var ub = new UserDefinedTypeBuilder()
                    .WithSchemaQualifiedName(schema, typeName)
                    .WithKind(kind)
                    .WithDbType(dbType)
                    .WithNativeTypeName(nativeTypeName)
                    .WithSystemType(systemTypeValue)
                    .WithMaxLength(maxLengthValue)
                    .WithPrecision(precisionValue)
                    .WithScale(scaleValue)
                    .WithIsUnicode(unicode)
                    .WithIsFixedLength(fixedLength);

                udts[userTypeId] = ub;

                // If this is a table type, store the mapping from type_table_object_id to user_type_id so we can link the table type columns
                // to the correct user-defined type builder in the second pass.
                if (isTableType && !reader.IsDBNull(ttObjOrdinal))
                    tableTypeObjectIds[reader.GetInt32(ttObjOrdinal)] = userTypeId;
            }
        }

        if (udts.Count == 0)
            return;

        // If there are any table types, we need to read their columns in a second pass since the column metadata is in a different
        // format and requires joining with sys.columns and sys.table_types.
        if (tableTypeObjectIds.Count > 0)
            await ReadTableTypeColumnsAsync(connection, udts, tableTypeObjectIds, cancellationToken).ConfigureAwait(false);

        // Now that we've read all the user-defined types and their columns for table types, we can build them and add them to the builder.
        foreach (var (_, ub) in udts)
            builder.AddUserDefinedType(ub.Build());
    }


    private static async Task ReadTableTypeColumnsAsync(
        SqlConnection connection,
        Dictionary<int, UserDefinedTypeBuilder> udts,
        Dictionary<int, int> tableTypeObjectIds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.object_id,
                c.column_id,
                c.name                      AS column_name,
                st.name                     AS system_type_name,
                TYPE_NAME(c.user_type_id)   AS user_type_name,
                c.max_length,
                c.precision,
                c.scale,
                c.is_nullable,
                c.is_identity,
                c.is_computed
            FROM sys.columns c
            INNER JOIN sys.table_types tt ON c.object_id = tt.type_table_object_id
            INNER JOIN sys.types st
                ON c.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            ORDER BY c.object_id, c.column_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var ttObjectId = reader.GetInt32(objectIdOrdinal);
            if (!tableTypeObjectIds.TryGetValue(ttObjectId, out var userTypeId))
                continue;
            if (!udts.TryGetValue(userTypeId, out var ub))
                continue;

            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.IsDBNull(userTypeOrdinal) ? systemTypeName : reader.GetString(userTypeOrdinal);
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var columnName = reader.GetString(colNameOrdinal);
            var columnId = reader.GetInt32(columnIdOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);
            var isIdentity = reader.GetBoolean(identityOrdinal);
            var isComputed = reader.GetBoolean(computedOrdinal);
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            ub.AddColumn(col => col
                .WithName(columnName)
                .WithOrdinalPosition(columnId)
                .WithIsNullable(isNullable)
                .WithIsIdentity(isIdentity)
                .WithIsComputed(isComputed)
                .WithNativeTypeName(nativeTypeName)
                .WithDbType(dbType)
                .WithSystemType(systemType)
                .WithMaxLength(maxLengthValue)
                .WithPrecision(precisionValue)
                .WithScale(scaleValue)
                .WithIsUnicode(isUnicode)
                .WithIsFixedLength(isFixedLength));
        }
    }

    private static async Task ReadParametersAsync<TBuilder>(
        SqlConnection connection,
        Dictionary<int, TBuilder> builders,
        string parentTable,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        await ReadParametersAsync(connection, builders, parentTable, null, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReadParametersAsync<TBuilder>(
        SqlConnection connection,
        Dictionary<int, TBuilder> builders,
        string parentTable,
        string? additionalFilter,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        var filterClause = additionalFilter is not null ? $" AND {additionalFilter}" : "";
        var sql = $"""
            SELECT
                par.object_id,
                par.name                        AS param_name,
                par.parameter_id,
                st.name                         AS system_type_name,
                TYPE_NAME(par.user_type_id)     AS user_type_name,
                par.max_length,
                par.precision,
                par.scale,
                par.is_output
            FROM sys.parameters par
            INNER JOIN sys.objects o ON par.object_id = o.object_id
            INNER JOIN sys.types st
                ON par.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            WHERE o.is_ms_shipped = 0 AND par.parameter_id > 0{filterClause}
            ORDER BY par.object_id, par.parameter_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int nameOrdinal = 1;
        const int paramIdOrdinal = 2;
        const int sysTypeOrdinal = 3;
        const int userTypeOrdinal = 4;
        const int maxLenOrdinal = 5;
        const int precisionOrdinal = 6;
        const int scaleOrdinal = 7;
        const int outputOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!builders.TryGetValue(objectId, out var b))
                continue;

            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.IsDBNull(userTypeOrdinal) ? systemTypeName : reader.GetString(userTypeOrdinal);
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            var paramName = reader.GetString(nameOrdinal);
            var paramOrdinal = reader.GetInt32(paramIdOrdinal);
            var isOutput = reader.GetBoolean(outputOrdinal);
            var direction = isOutput ? Metadata.ParameterDirection.Output : Metadata.ParameterDirection.Input;

            Action<ParameterBuilder> configure = p => p
                .WithName(paramName)
                .WithOrdinal(paramOrdinal)
                .WithDirection(direction)
                .WithNativeTypeName(nativeTypeName)
                .WithDbType(dbType)
                .WithSystemType(systemType)
                .WithMaxLength(maxLengthValue)
                .WithPrecision(precisionValue)
                .WithScale(scaleValue)
                .WithIsUnicode(isUnicode)
                .WithIsFixedLength(isFixedLength);

            if (b is StoredProcedureBuilder spb)
                spb.AddParameter(configure);
            else if (b is TableValuedFunctionBuilder tvfb)
                tvfb.AddParameter(configure);
        }
    }


    private static string BuildTableFilter(SchemaReaderOptions options)
    {
        // Always filter out system objects (is_ms_shipped = 0) and then apply additional filters based on the specified schemas and tables if provided.
        var conditions = new List<string> { "t.is_ms_shipped = 0" };

        // If specific schemas are specified in the options, add a filter condition to include only those schemas.
        if (options.Schemas.Count > 0)
        {
            var list = string.Join(", ", options.Schemas.Select(EscapeLiteral));
            conditions.Add($"SCHEMA_NAME(t.schema_id) IN ({list})");
        }

        // If specific tables are specified in the options, add a filter condition to include only those tables.
        if (options.Tables.Count > 0)
        {
            var list = string.Join(", ", options.Tables.Select(EscapeLiteral));
            conditions.Add($"t.name IN ({list})");
        }

        // Combine all conditions into a single WHERE clause string, joining them with "AND".
        return string.Join("\n    AND ", conditions);
    }

    private static string? BuildSchemaFilter(IReadOnlyCollection<string> schemas, string schemaExpression)
    {
        if (schemas.Count == 0)
            return null;

        // Build a filter condition to include only the specified schemas.
        // The schemaExpression parameter allows specifying the expression to use
        // for the schema name (e.g. "SCHEMA_NAME(o.schema_id)" or "SCHEMA_NAME(t.schema_id)").

        var list = string.Join(", ", schemas.Select(EscapeLiteral));

        // Return a filter condition like "SCHEMA_NAME(o.schema_id) IN ('schema1', 'schema2')".
        return $"{schemaExpression} IN ({list})";
    }

    private static string EscapeLiteral(string s) => $"N'{s.Replace("'", "''")}'";

    private static (DbType DbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapSqlServerType(string typeName)
        => typeName.ToLowerInvariant() switch
        {
            "bigint" => (DbType.Int64, typeof(long), null, null),
            "int" => (DbType.Int32, typeof(int), null, null),
            "smallint" => (DbType.Int16, typeof(short), null, null),
            "tinyint" => (DbType.Byte, typeof(byte), null, null),
            "bit" => (DbType.Boolean, typeof(bool), null, null),
            "decimal" or "numeric" => (DbType.Decimal, typeof(decimal), null, null),
            "money" or "smallmoney" => (DbType.Currency, typeof(decimal), null, null),
            "float" => (DbType.Double, typeof(double), null, null),
            "real" => (DbType.Single, typeof(float), null, null),
            "datetime" or "smalldatetime" => (DbType.DateTime, typeof(DateTime), null, null),
            "datetime2" => (DbType.DateTime2, typeof(DateTime), null, null),
            "datetimeoffset" => (DbType.DateTimeOffset, typeof(DateTimeOffset), null, null),
            "char" => (DbType.AnsiStringFixedLength, typeof(string), false, true),
            "varchar" => (DbType.AnsiString, typeof(string), false, false),
            "text" => (DbType.AnsiString, typeof(string), false, false),
            "nchar" => (DbType.StringFixedLength, typeof(string), true, true),
            "nvarchar" => (DbType.String, typeof(string), true, false),
            "ntext" => (DbType.String, typeof(string), true, false),
            "json" => (DbType.String, typeof(string), true, false),
            "binary" => (DbType.Binary, typeof(byte[]), null, true),
            "varbinary" or "image" => (DbType.Binary, typeof(byte[]), null, false),
            "timestamp" or "rowversion" => (DbType.Binary, typeof(byte[]), null, null),
            "uniqueidentifier" => (DbType.Guid, typeof(Guid), null, null),
            "xml" => (DbType.Xml, typeof(string), null, null),
            "sql_variant" => (DbType.Object, typeof(object), null, null),
#if NET6_0_OR_GREATER
            "time" => (DbType.Time, typeof(TimeOnly), null, null),
            "date" => (DbType.Date, typeof(DateOnly), null, null),
#endif
            _ => (DbType.Object, typeof(object), null, null),
        };

    private static string FormatNativeTypeName(string systemTypeName, string userTypeName, short maxLength, byte precision, byte scale)
    {
        // User-defined alias type — return the alias name as-is.
        if (!string.Equals(systemTypeName, userTypeName, StringComparison.OrdinalIgnoreCase))
            return userTypeName;

        var lower = systemTypeName.ToLowerInvariant();

        return lower switch
        {
            "char" or "varchar" or "binary" or "varbinary"
                => maxLength == -1 ? $"{lower}(max)" : $"{lower}({maxLength})",
            "nchar" or "nvarchar"
                => maxLength == -1 ? $"{lower}(max)" : $"{lower}({maxLength / 2})",
            "decimal" or "numeric"
                => $"{lower}({precision}, {scale})",
            "datetime2" or "datetimeoffset" or "time"
                => scale != 7 ? $"{lower}({scale})" : lower,
            _ => lower,
        };
    }

    private static int? NormalizeMaxLength(string systemTypeName, short maxLength)
        => systemTypeName.ToLowerInvariant() switch
        {
            "char" or "varchar" or "binary" or "varbinary"
                => maxLength == -1 ? null : (int?)maxLength,
            "nchar" or "nvarchar"
                => maxLength == -1 ? null : (int?)(maxLength / 2),
            _ => null,
        };

    private static bool HasPrecision(string systemTypeName)
        => systemTypeName.ToLowerInvariant() is "decimal" or "numeric";

    private static bool HasScale(string systemTypeName)
        => systemTypeName.ToLowerInvariant() is "decimal" or "numeric" or "datetime2" or "datetimeoffset" or "time";

    private static ReferentialAction MapReferentialAction(byte action) => action switch
    {
        1 => ReferentialAction.Cascade,
        2 => ReferentialAction.SetNull,
        3 => ReferentialAction.SetDefault,
        _ => ReferentialAction.NoAction,
    };
}
