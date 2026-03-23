using System.Data;
using System.Data.Common;

using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

/// <summary>
/// Reads structural metadata from a SQL Server database using <c>sys.*</c> catalog views.
/// All queries are bulk-loaded (one query per data type across all objects) to minimize
/// database round trips. Extended properties are mapped to descriptions and annotations.
/// </summary>
public sealed class SqlServerSchemaReader : DatabaseSchemaReader<SqlConnection>
{
    private const string MsDescription = "MS_Description";

    /// <inheritdoc />
    public override string ProviderName => "SqlServer";

    // ──────────────────────────────────────────────────────────────────
    //  Database metadata
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                CAST(SERVERPROPERTY('Collation') AS NVARCHAR(256)),
                SCHEMA_NAME(),
                @@VERSION
            """;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            builder
                .WithCollation(reader.IsDBNull(0) ? null : reader.GetString(0))
                .WithDefaultSchemaName(reader.IsDBNull(1) ? null : reader.GetString(1))
                .WithServerVersion(reader.IsDBNull(2) ? null : reader.GetString(2));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Tables
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadTablesAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Phase 1 — Discover tables
        var tables = new Dictionary<int, TableBuilder>();
        var objectIds = new HashSet<int>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    t.object_id,
                    s.name,
                    t.name,
                    t.temporal_type,
                    t.is_memory_optimized,
                    t.is_filetable,
                    SCHEMA_NAME(ht.schema_id),
                    ht.name
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                LEFT JOIN sys.tables ht ON t.history_table_id = ht.object_id
                WHERE t.is_ms_shipped = 0
                  AND t.temporal_type <> 1
                ORDER BY s.name, t.name
                """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                var schema = reader.GetString(1);
                var name = reader.GetString(2);

                if (!ShouldIncludeTable(schema, name, options))
                    continue;

                var temporalType = reader.GetByte(3);
                var isMemoryOptimized = reader.GetBoolean(4);
                var isFileTable = reader.GetBoolean(5);
                var historySchema = reader.IsDBNull(6) ? null : reader.GetString(6);
                var historyName = reader.IsDBNull(7) ? null : reader.GetString(7);

                var tb = new TableBuilder()
                    .WithSchemaQualifiedName(schema, name)
                    .WithOptions(new TableOptions
                    {
                        IsTemporalTable = temporalType == 2,
                        HistoryTableName = historySchema is not null && historyName is not null
                            ? new SchemaQualifiedName { Schema = historySchema, Name = historyName }
                            : null,
                        IsMemoryOptimized = isMemoryOptimized,
                        IsFileTable = isFileTable,
                    });

                tables[objectId] = tb;
                objectIds.Add(objectId);
            }
        }

        if (tables.Count == 0)
            return;

        // Phase 2 — Extended properties (loaded early so column descriptions are available)
        var extProps = await ReadExtendedPropertiesAsync(
            connection,
            "INNER JOIN sys.tables t ON ep.major_id = t.object_id AND t.is_ms_shipped = 0",
            cancellationToken).ConfigureAwait(false);

        // Phase 3 — Columns
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    c.object_id,
                    c.column_id,
                    c.name,
                    st.name,
                    TYPE_NAME(c.user_type_id),
                    c.max_length,
                    c.precision,
                    c.scale,
                    c.is_nullable,
                    c.is_identity,
                    c.is_computed,
                    c.is_rowversion,
                    c.collation_name,
                    CAST(ic.seed_value AS BIGINT),
                    CAST(ic.increment_value AS BIGINT),
                    cc.definition,
                    cc.is_persisted,
                    dc.definition
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
                WHERE t.is_ms_shipped = 0
                ORDER BY c.object_id, c.column_id
                """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (!tables.TryGetValue(objectId, out var tb))
                    continue;

                var columnId = reader.GetInt32(1);
                var columnName = reader.GetString(2);
                var systemTypeName = reader.GetString(3);
                var userTypeName = reader.IsDBNull(4) ? systemTypeName : reader.GetString(4);
                var maxLength = reader.GetInt16(5);
                var precision = reader.GetByte(6);
                var scale = reader.GetByte(7);
                var isNullable = reader.GetBoolean(8);
                var isIdentity = reader.GetBoolean(9);
                var isComputed = reader.GetBoolean(10);
                var isRowVersion = reader.GetBoolean(11);
                var collation = reader.IsDBNull(12) ? null : reader.GetString(12);
                var identitySeed = reader.IsDBNull(13) ? (long?)null : reader.GetInt64(13);
                var identityIncrement = reader.IsDBNull(14) ? (long?)null : reader.GetInt64(14);
                var computedSql = reader.IsDBNull(15) ? null : reader.GetString(15);
                var isPersisted = !reader.IsDBNull(16) && reader.GetBoolean(16);
                var defaultSql = reader.IsDBNull(17) ? null : reader.GetString(17);

                var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);
                var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

                // Column-level extended properties
                string? description = null;
                Dictionary<string, object?>? colAnnotations = null;
                if (extProps.TryGetValue(objectId, out var eps))
                {
                    foreach (var (minorId, epName, epValue) in eps)
                    {
                        if (minorId != columnId)
                            continue;

                        if (epName == MsDescription)
                            description = epValue;
                        else
                        {
                            colAnnotations ??= [];
                            colAnnotations[epName] = epValue;
                        }
                    }
                }

                tb.AddColumn(col =>
                {
                    col.WithName(columnName)
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
                        .WithMaxLength(NormalizeMaxLength(systemTypeName, maxLength))
                        .WithPrecision(HasPrecision(systemTypeName) ? precision : null)
                        .WithScale(HasScale(systemTypeName) ? (int?)scale : null)
                        .WithIsUnicode(isUnicode)
                        .WithIsFixedLength(isFixedLength);

                    if (colAnnotations is not null)
                    {
                        foreach (var (k, v) in colAnnotations)
                            col.WithAnnotation(k, v);
                    }
                });
            }
        }

        // Phase 4 — Key constraints (PK + UQ)
        var keyConstraints = new Dictionary<(int ObjectId, string Name), (string Type, bool IsClustered, List<ColumnReference> Columns)>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    kc.parent_object_id,
                    kc.name,
                    kc.type,
                    i.type,
                    c.name,
                    ic.is_descending_key
                FROM sys.key_constraints kc
                INNER JOIN sys.indexes i
                    ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
                INNER JOIN sys.index_columns ic
                    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c
                    ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE ic.is_included_column = 0
                ORDER BY kc.parent_object_id, kc.name, ic.key_ordinal
                """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (!objectIds.Contains(objectId))
                    continue;

                var constraintName = reader.GetString(1);
                var key = (objectId, constraintName);

                if (!keyConstraints.TryGetValue(key, out var kc))
                {
                    var type = reader.GetString(2).Trim();
                    var indexType = reader.GetByte(3);
                    kc = (type, indexType == 1, []);
                    keyConstraints[key] = kc;
                }

                kc.Columns.Add(new ColumnReference
                {
                    ColumnName = reader.GetString(4),
                    SortDirection = reader.GetBoolean(5) ? SortDirection.Descending : SortDirection.Ascending,
                });
            }
        }

        foreach (var ((objectId, name), (type, isClustered, columns)) in keyConstraints)
        {
            var tb = tables[objectId];
            if (type == "PK")
                tb.WithPrimaryKey(name, isClustered, [.. columns]);
            else
                tb.AddUniqueConstraint(name, [.. columns]);
        }

        // Phase 5 — Indexes (non-PK, non-UQ)
        var indexes = new Dictionary<(int ObjectId, int IndexId), (int ObjectId, IndexBuilder Builder)>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    i.object_id,
                    i.index_id,
                    i.name,
                    i.is_unique,
                    i.type,
                    i.is_disabled,
                    i.has_filter,
                    i.filter_definition,
                    c.name,
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

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (!objectIds.Contains(objectId))
                    continue;

                var indexId = reader.GetInt32(1);
                var key = (objectId, indexId);

                if (!indexes.TryGetValue(key, out var entry))
                {
                    var ib = new IndexBuilder()
                        .WithName(reader.GetString(2))
                        .WithIsUnique(reader.GetBoolean(3))
                        .WithIsClustered(reader.GetByte(4) == 1)
                        .WithIsDisabled(reader.GetBoolean(5));

                    if (reader.GetBoolean(6))
                    {
                        ib.WithIsFiltered(true);
                        if (!reader.IsDBNull(7))
                            ib.WithFilterExpression(reader.GetString(7));
                    }

                    var indexType = reader.GetByte(4);
                    if (indexType is 5 or 6)
                        ib.WithIndexType("COLUMNSTORE");
                    else if (indexType == 7)
                        ib.WithIndexType("HASH");

                    entry = (objectId, ib);
                    indexes[key] = entry;
                }

                var columnName = reader.GetString(8);
                var isDescending = reader.GetBoolean(9);
                var isIncluded = reader.GetBoolean(10);

                if (isIncluded)
                    entry.Builder.AddIncludedColumn(columnName);
                else
                    entry.Builder.AddColumn(columnName, isDescending ? SortDirection.Descending : SortDirection.Ascending);
            }
        }

        foreach (var (_, (objectId, ib)) in indexes)
            tables[objectId].AddIndex(ib.Build());

        // Phase 6 — Check constraints
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT cc.parent_object_id, cc.name, cc.definition
                FROM sys.check_constraints cc
                INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
                WHERE t.is_ms_shipped = 0
                ORDER BY cc.parent_object_id, cc.name
                """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (objectIds.Contains(objectId))
                    tables[objectId].AddCheckConstraint(reader.GetString(1), reader.GetString(2));
            }
        }

        // Phase 7 — Foreign keys
        var foreignKeys = new Dictionary<(int ObjectId, string Name), (int ObjectId, ForeignKeyBuilder Builder)>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    fk.parent_object_id,
                    fk.name,
                    SCHEMA_NAME(rt.schema_id),
                    rt.name,
                    fk.delete_referential_action,
                    fk.update_referential_action,
                    fk.is_disabled,
                    pc.name,
                    rc.name
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

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (!objectIds.Contains(objectId))
                    continue;

                var fkName = reader.GetString(1);
                var key = (objectId, fkName);

                if (!foreignKeys.TryGetValue(key, out var entry))
                {
                    var fkb = new ForeignKeyBuilder()
                        .WithName(fkName)
                        .WithPrincipalTableName(reader.GetString(2), reader.GetString(3))
                        .WithOnDelete(MapReferentialAction(reader.GetByte(4)))
                        .WithOnUpdate(MapReferentialAction(reader.GetByte(5)))
                        .WithIsDisabled(reader.GetBoolean(6));

                    entry = (objectId, fkb);
                    foreignKeys[key] = entry;
                }

                entry.Builder.AddColumnMapping(reader.GetString(7), reader.GetString(8));
            }
        }

        foreach (var (_, (objectId, fkb)) in foreignKeys)
            tables[objectId].AddForeignKey(fkb.Build());

        // Phase 8 — Triggers
        var triggerData = new Dictionary<(int ObjectId, string Name), (bool IsDisabled, bool IsInsteadOf, string? Definition, List<string> Events)>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = options.IncludeDefinitions
                ? """
                    SELECT
                        tr.parent_id,
                        tr.name,
                        tr.is_disabled,
                        tr.is_instead_of_trigger,
                        m.definition,
                        te.type_desc
                    FROM sys.triggers tr
                    INNER JOIN sys.trigger_events te ON tr.object_id = te.object_id
                    LEFT JOIN sys.sql_modules m ON tr.object_id = m.object_id
                    WHERE tr.parent_class = 1
                    ORDER BY tr.parent_id, tr.name
                    """
                : """
                    SELECT
                        tr.parent_id,
                        tr.name,
                        tr.is_disabled,
                        tr.is_instead_of_trigger,
                        NULL,
                        te.type_desc
                    FROM sys.triggers tr
                    INNER JOIN sys.trigger_events te ON tr.object_id = te.object_id
                    WHERE tr.parent_class = 1
                    ORDER BY tr.parent_id, tr.name
                    """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var parentId = reader.GetInt32(0);
                if (!objectIds.Contains(parentId))
                    continue;

                var name = reader.GetString(1);
                var key = (parentId, name);

                if (!triggerData.TryGetValue(key, out var td))
                {
                    td = (reader.GetBoolean(2), reader.GetBoolean(3),
                          reader.IsDBNull(4) ? null : reader.GetString(4), []);
                    triggerData[key] = td;
                }

                td.Events.Add(reader.GetString(5));
            }
        }

        foreach (var ((parentId, name), (isDisabled, isInsteadOf, definition, events)) in triggerData)
        {
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

            tables[parentId].AddTrigger(new Trigger
            {
                Name = name,
                Timing = isInsteadOf ? TriggerTiming.InsteadOf : TriggerTiming.After,
                Events = triggerEvents,
                Definition = definition,
                IsDisabled = isDisabled,
            });
        }

        // Phase 9 — Apply table-level extended properties and build
        foreach (var (objectId, tb) in tables)
        {
            if (extProps.TryGetValue(objectId, out var eps))
            {
                foreach (var (minorId, epName, value) in eps)
                {
                    if (minorId != 0)
                        continue;

                    if (epName == MsDescription)
                        tb.WithDescription(value);
                    else
                        tb.WithAnnotation(epName, value);
                }
            }

            builder.AddTable(tb.Build());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Views
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadViewsAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Phase 1 — Discover views
        var views = new Dictionary<int, ViewBuilder>();
        var objectIds = new HashSet<int>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = options.IncludeDefinitions
                ? """
                    SELECT
                        v.object_id,
                        s.name,
                        v.name,
                        m.definition,
                        CASE WHEN EXISTS (
                            SELECT 1 FROM sys.indexes i
                            WHERE i.object_id = v.object_id AND i.type = 1
                        ) THEN 1 ELSE 0 END
                    FROM sys.views v
                    INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
                    LEFT JOIN sys.sql_modules m ON v.object_id = m.object_id
                    WHERE v.is_ms_shipped = 0
                    ORDER BY s.name, v.name
                    """
                : """
                    SELECT
                        v.object_id,
                        s.name,
                        v.name,
                        NULL,
                        CASE WHEN EXISTS (
                            SELECT 1 FROM sys.indexes i
                            WHERE i.object_id = v.object_id AND i.type = 1
                        ) THEN 1 ELSE 0 END
                    FROM sys.views v
                    INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
                    WHERE v.is_ms_shipped = 0
                    ORDER BY s.name, v.name
                    """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                var schema = reader.GetString(1);
                var name = reader.GetString(2);

                if (!ShouldIncludeSchema(schema, options))
                    continue;

                var vb = new ViewBuilder()
                    .WithSchemaQualifiedName(schema, name)
                    .WithDefinition(reader.IsDBNull(3) ? null : reader.GetString(3))
                    .WithIsMaterialized(reader.GetInt32(4) == 1);

                views[objectId] = vb;
                objectIds.Add(objectId);
            }
        }

        if (views.Count == 0)
            return;

        // Phase 2 — Extended properties
        var extProps = await ReadExtendedPropertiesAsync(
            connection,
            "INNER JOIN sys.views v ON ep.major_id = v.object_id AND v.is_ms_shipped = 0",
            cancellationToken).ConfigureAwait(false);

        // Phase 3 — View columns
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    c.object_id,
                    c.column_id,
                    c.name,
                    st.name,
                    TYPE_NAME(c.user_type_id),
                    c.max_length,
                    c.precision,
                    c.scale,
                    c.is_nullable,
                    c.collation_name
                FROM sys.columns c
                INNER JOIN sys.views v ON c.object_id = v.object_id
                INNER JOIN sys.types st
                    ON c.system_type_id = st.system_type_id
                    AND st.system_type_id = st.user_type_id
                WHERE v.is_ms_shipped = 0
                ORDER BY c.object_id, c.column_id
                """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (!views.TryGetValue(objectId, out var vb))
                    continue;

                var columnId = reader.GetInt32(1);
                var columnName = reader.GetString(2);
                var systemTypeName = reader.GetString(3);
                var userTypeName = reader.IsDBNull(4) ? systemTypeName : reader.GetString(4);
                var maxLength = reader.GetInt16(5);
                var precision = reader.GetByte(6);
                var scale = reader.GetByte(7);
                var isNullable = reader.GetBoolean(8);
                var collation = reader.IsDBNull(9) ? null : reader.GetString(9);

                var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);
                var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

                string? description = null;
                Dictionary<string, object?>? colAnnotations = null;
                if (extProps.TryGetValue(objectId, out var eps))
                {
                    foreach (var (minorId, epName, epValue) in eps)
                    {
                        if (minorId != columnId)
                            continue;

                        if (epName == MsDescription)
                            description = epValue;
                        else
                        {
                            colAnnotations ??= [];
                            colAnnotations[epName] = epValue;
                        }
                    }
                }

                vb.AddColumn(col =>
                {
                    col.WithName(columnName)
                        .WithOrdinalPosition(columnId)
                        .WithIsNullable(isNullable)
                        .WithCollation(collation)
                        .WithDescription(description)
                        .WithNativeTypeName(nativeTypeName)
                        .WithDbType(dbType)
                        .WithSystemType(systemType)
                        .WithMaxLength(NormalizeMaxLength(systemTypeName, maxLength))
                        .WithPrecision(HasPrecision(systemTypeName) ? precision : null)
                        .WithScale(HasScale(systemTypeName) ? (int?)scale : null)
                        .WithIsUnicode(isUnicode)
                        .WithIsFixedLength(isFixedLength);

                    if (colAnnotations is not null)
                    {
                        foreach (var (k, v) in colAnnotations)
                            col.WithAnnotation(k, v);
                    }
                });
            }
        }

        // Phase 4 — Build views
        foreach (var (objectId, vb) in views)
        {
            if (extProps.TryGetValue(objectId, out var eps))
            {
                foreach (var (minorId, epName, value) in eps)
                {
                    if (minorId != 0)
                        continue;

                    if (epName == MsDescription)
                        vb.WithDescription(value);
                    else
                        vb.WithAnnotation(epName, value);
                }
            }

            builder.AddView(vb.Build());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Sequences
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadSequencesAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                s.name,
                sc.name,
                TYPE_NAME(s.system_type_id),
                CAST(s.start_value AS BIGINT),
                CAST(s.increment AS BIGINT),
                CAST(s.minimum_value AS BIGINT),
                CAST(s.maximum_value AS BIGINT),
                s.is_cycling,
                s.cache_size,
                s.is_cached
            FROM sys.sequences s
            INNER JOIN sys.schemas sc ON s.schema_id = sc.schema_id
            ORDER BY sc.name, s.name
            """;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(1);
            if (!ShouldIncludeSchema(schema, options))
                continue;

            var seqName = reader.GetString(0);
            var typeName = reader.GetString(2);
            var (dbType, systemType, _, _) = MapSqlServerType(typeName);

            builder.AddSequence(seq => seq
                .WithSchemaQualifiedName(schema, seqName)
                .WithDbType(dbType)
                .WithSystemType(systemType)
                .WithStartValue(reader.GetInt64(3))
                .WithIncrement(reader.GetInt64(4))
                .WithMinValue(reader.GetInt64(5))
                .WithMaxValue(reader.GetInt64(6))
                .WithIsCycling(reader.GetBoolean(7))
                .WithCacheSize(reader.GetBoolean(9) ? reader.IsDBNull(8) ? null : reader.GetInt32(8) : null));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Stored procedures
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadStoredProceduresAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Phase 1 — Procedures
        var procs = new Dictionary<int, StoredProcedureBuilder>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = options.IncludeDefinitions
                ? """
                    SELECT p.object_id, s.name, p.name, m.definition
                    FROM sys.procedures p
                    INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
                    LEFT JOIN sys.sql_modules m ON p.object_id = m.object_id
                    WHERE p.is_ms_shipped = 0
                    ORDER BY s.name, p.name
                    """
                : """
                    SELECT p.object_id, s.name, p.name, NULL
                    FROM sys.procedures p
                    INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
                    WHERE p.is_ms_shipped = 0
                    ORDER BY s.name, p.name
                    """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var schema = reader.GetString(1);
                if (!ShouldIncludeSchema(schema, options))
                    continue;

                var objectId = reader.GetInt32(0);
                var spb = new StoredProcedureBuilder()
                    .WithSchemaQualifiedName(schema, reader.GetString(2))
                    .WithDefinition(reader.IsDBNull(3) ? null : reader.GetString(3));

                procs[objectId] = spb;
            }
        }

        if (procs.Count == 0)
            return;

        // Phase 2 — Parameters
        await ReadParametersAsync(connection, procs, "sys.procedures", cancellationToken).ConfigureAwait(false);

        // Phase 3 — Extended properties (descriptions)
        var extProps = await ReadExtendedPropertiesAsync(
            connection,
            "INNER JOIN sys.procedures p ON ep.major_id = p.object_id AND p.is_ms_shipped = 0",
            cancellationToken).ConfigureAwait(false);

        foreach (var (objectId, spb) in procs)
        {
            if (extProps.TryGetValue(objectId, out var eps))
            {
                foreach (var (minorId, epName, value) in eps)
                {
                    if (minorId == 0 && epName == MsDescription)
                        spb.WithDescription(value);
                    else if (minorId == 0)
                        spb.WithAnnotation(epName, value);
                }
            }

            builder.AddStoredProcedure(spb.Build());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Scalar functions
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadScalarFunctionsAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var funcs = new Dictionary<int, ScalarFunctionBuilder>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = options.IncludeDefinitions
                ? """
                    SELECT o.object_id, s.name, o.name, m.definition
                    FROM sys.objects o
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                    LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
                    WHERE o.type = 'FN' AND o.is_ms_shipped = 0
                    ORDER BY s.name, o.name
                    """
                : """
                    SELECT o.object_id, s.name, o.name, NULL
                    FROM sys.objects o
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                    WHERE o.type = 'FN' AND o.is_ms_shipped = 0
                    ORDER BY s.name, o.name
                    """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var schema = reader.GetString(1);
                if (!ShouldIncludeSchema(schema, options))
                    continue;

                var objectId = reader.GetInt32(0);
                var fb = new ScalarFunctionBuilder()
                    .WithSchemaQualifiedName(schema, reader.GetString(2))
                    .WithDefinition(reader.IsDBNull(3) ? null : reader.GetString(3));

                funcs[objectId] = fb;
            }
        }

        if (funcs.Count == 0)
            return;

        // Parameters + return type (parameter_id = 0 is the return value)
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    par.object_id,
                    par.name,
                    par.parameter_id,
                    st.name,
                    TYPE_NAME(par.user_type_id),
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

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (!funcs.TryGetValue(objectId, out var fb))
                    continue;

                var paramId = reader.GetInt32(2);
                var systemTypeName = reader.GetString(3);
                var userTypeName = reader.IsDBNull(4) ? systemTypeName : reader.GetString(4);
                var maxLength = reader.GetInt16(5);
                var precision = reader.GetByte(6);
                var scale = reader.GetByte(7);

                var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);
                var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

                if (paramId == 0)
                {
                    // Return type
                    fb.WithReturnType(new TypeMapping
                    {
                        DbType = dbType,
                        NativeTypeName = nativeTypeName,
                        SystemType = systemType,
                        MaxLength = NormalizeMaxLength(systemTypeName, maxLength),
                        Precision = HasPrecision(systemTypeName) ? precision : null,
                        Scale = HasScale(systemTypeName) ? (int?)scale : null,
                        IsUnicode = isUnicode,
                        IsFixedLength = isFixedLength,
                    });
                }
                else
                {
                    fb.AddParameter(p => p
                        .WithName(reader.GetString(1))
                        .WithOrdinal(paramId)
                        .WithDirection(reader.GetBoolean(8) ? Metadata.ParameterDirection.Output : Metadata.ParameterDirection.Input)
                        .WithNativeTypeName(nativeTypeName)
                        .WithDbType(dbType)
                        .WithSystemType(systemType)
                        .WithMaxLength(NormalizeMaxLength(systemTypeName, maxLength))
                        .WithPrecision(HasPrecision(systemTypeName) ? precision : null)
                        .WithScale(HasScale(systemTypeName) ? (int?)scale : null)
                        .WithIsUnicode(isUnicode)
                        .WithIsFixedLength(isFixedLength));
                }
            }
        }

        foreach (var (_, fb) in funcs)
            builder.AddScalarFunction(fb.Build());
    }

    // ──────────────────────────────────────────────────────────────────
    //  Table-valued functions
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadTableValuedFunctionsAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var funcs = new Dictionary<int, TableValuedFunctionBuilder>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = options.IncludeDefinitions
                ? """
                    SELECT o.object_id, s.name, o.name, m.definition
                    FROM sys.objects o
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                    LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
                    WHERE o.type IN ('TF', 'IF') AND o.is_ms_shipped = 0
                    ORDER BY s.name, o.name
                    """
                : """
                    SELECT o.object_id, s.name, o.name, NULL
                    FROM sys.objects o
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                    WHERE o.type IN ('TF', 'IF') AND o.is_ms_shipped = 0
                    ORDER BY s.name, o.name
                    """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var schema = reader.GetString(1);
                if (!ShouldIncludeSchema(schema, options))
                    continue;

                var objectId = reader.GetInt32(0);
                var fb = new TableValuedFunctionBuilder()
                    .WithSchemaQualifiedName(schema, reader.GetString(2))
                    .WithDefinition(reader.IsDBNull(3) ? null : reader.GetString(3));

                funcs[objectId] = fb;
            }
        }

        if (funcs.Count == 0)
            return;

        // Parameters (parameter_id > 0 only; TVFs have no return-value parameter)
        await ReadParametersAsync(connection, funcs, "sys.objects o2", "o.type IN ('TF', 'IF')", cancellationToken).ConfigureAwait(false);

        // Return columns
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    c.object_id,
                    c.column_id,
                    c.name,
                    st.name,
                    TYPE_NAME(c.user_type_id),
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

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(0);
                if (!funcs.TryGetValue(objectId, out var fb))
                    continue;

                var systemTypeName = reader.GetString(3);
                var userTypeName = reader.IsDBNull(4) ? systemTypeName : reader.GetString(4);
                var maxLength = reader.GetInt16(5);
                var precision = reader.GetByte(6);
                var scale = reader.GetByte(7);

                var (dbType, systemType, _, _) = MapSqlServerType(systemTypeName);
                var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

                fb.AddReturnColumn(
                    reader.GetString(2),
                    reader.GetInt32(1),
                    dbType,
                    nativeTypeName,
                    systemType,
                    reader.GetBoolean(8));
            }
        }

        foreach (var (_, fb) in funcs)
            builder.AddTableValuedFunction(fb.Build());
    }

    // ──────────────────────────────────────────────────────────────────
    //  User-defined types
    // ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task ReadUserDefinedTypesAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var udts = new Dictionary<int, UserDefinedTypeBuilder>();
        var tableTypeObjectIds = new Dictionary<int, int>(); // type_table_object_id -> user_type_id

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    t.user_type_id,
                    s.name,
                    t.name,
                    t.is_table_type,
                    st.name,
                    t.max_length,
                    t.precision,
                    t.scale,
                    t.is_nullable,
                    tt.type_table_object_id
                FROM sys.types t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.types st
                    ON t.system_type_id = st.system_type_id
                    AND st.system_type_id = st.user_type_id
                LEFT JOIN sys.table_types tt ON t.user_type_id = tt.user_type_id
                WHERE t.is_user_defined = 1
                ORDER BY s.name, t.name
                """;

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var schema = reader.GetString(1);
                if (!ShouldIncludeSchema(schema, options))
                    continue;

                var userTypeId = reader.GetInt32(0);
                var isTableType = reader.GetBoolean(3);
                var baseTypeName = reader.GetString(4);
                var maxLength = reader.GetInt16(5);
                var precision = reader.GetByte(6);
                var scale = reader.GetByte(7);

                var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(baseTypeName);

                var ub = new UserDefinedTypeBuilder()
                    .WithSchemaQualifiedName(schema, reader.GetString(2))
                    .WithKind(isTableType ? UserDefinedTypeKind.TableType : UserDefinedTypeKind.Alias)
                    .WithDbType(dbType)
                    .WithNativeTypeName(isTableType ? "table" : FormatNativeTypeName(baseTypeName, baseTypeName, maxLength, precision, scale))
                    .WithSystemType(isTableType ? typeof(object) : systemType)
                    .WithMaxLength(isTableType ? null : NormalizeMaxLength(baseTypeName, maxLength))
                    .WithPrecision(HasPrecision(baseTypeName) ? precision : null)
                    .WithScale(HasScale(baseTypeName) ? (int?)scale : null)
                    .WithIsUnicode(isTableType ? null : isUnicode)
                    .WithIsFixedLength(isTableType ? null : isFixedLength);

                udts[userTypeId] = ub;

                if (isTableType && !reader.IsDBNull(9))
                    tableTypeObjectIds[reader.GetInt32(9)] = userTypeId;
            }
        }

        if (udts.Count == 0)
            return;

        // Table-type columns
        if (tableTypeObjectIds.Count > 0)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT
                    c.object_id,
                    c.column_id,
                    c.name,
                    st.name,
                    TYPE_NAME(c.user_type_id),
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

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var ttObjectId = reader.GetInt32(0);
                if (!tableTypeObjectIds.TryGetValue(ttObjectId, out var userTypeId))
                    continue;
                if (!udts.TryGetValue(userTypeId, out var ub))
                    continue;

                var systemTypeName = reader.GetString(3);
                var userTypeName = reader.IsDBNull(4) ? systemTypeName : reader.GetString(4);
                var maxLength = reader.GetInt16(5);
                var precision = reader.GetByte(6);
                var scale = reader.GetByte(7);

                var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);
                var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

                ub.AddColumn(col => col
                    .WithName(reader.GetString(2))
                    .WithOrdinalPosition(reader.GetInt32(1))
                    .WithIsNullable(reader.GetBoolean(8))
                    .WithIsIdentity(reader.GetBoolean(9))
                    .WithIsComputed(reader.GetBoolean(10))
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(NormalizeMaxLength(systemTypeName, maxLength))
                    .WithPrecision(HasPrecision(systemTypeName) ? precision : null)
                    .WithScale(HasScale(systemTypeName) ? (int?)scale : null)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength));
            }
        }

        foreach (var (_, ub) in udts)
            builder.AddUserDefinedType(ub.Build());
    }

    // ──────────────────────────────────────────────────────────────────
    //  Shared helpers
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads extended properties (class = 1) for objects matched by <paramref name="joinClause"/>.
    /// Returns a lookup keyed by object_id containing (minor_id, property name, value) tuples.
    /// </summary>
    private static async Task<Dictionary<int, List<(int MinorId, string Name, string Value)>>>
        ReadExtendedPropertiesAsync(
            SqlConnection connection,
            string joinClause,
            CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, List<(int, string, string)>>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT ep.major_id, ep.minor_id, ep.name, CAST(ep.value AS NVARCHAR(4000))
            FROM sys.extended_properties ep
            {joinClause}
            WHERE ep.class = 1
            ORDER BY ep.major_id, ep.minor_id, ep.name
            """;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var majorId = reader.GetInt32(0);
            var minorId = reader.GetInt32(1);
            var name = reader.GetString(2);
            var value = reader.IsDBNull(3) ? "" : reader.GetString(3);

            if (!result.TryGetValue(majorId, out var list))
            {
                list = [];
                result[majorId] = list;
            }

            list.Add((minorId, name, value));
        }

        return result;
    }

    /// <summary>
    /// Bulk-reads parameters for stored procedures and adds them to the corresponding builders.
    /// </summary>
    private static async Task ReadParametersAsync<TBuilder>(
        SqlConnection connection,
        Dictionary<int, TBuilder> builders,
        string parentTable,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        await ReadParametersAsync(connection, builders, parentTable, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Bulk-reads parameters for objects matching the specified parent table and optional filter.
    /// </summary>
    private static async Task ReadParametersAsync<TBuilder>(
        SqlConnection connection,
        Dictionary<int, TBuilder> builders,
        string parentTable,
        string? additionalFilter,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        using var cmd = connection.CreateCommand();

        var filterClause = additionalFilter is not null ? $" AND {additionalFilter}" : "";

        cmd.CommandText = $"""
            SELECT
                par.object_id,
                par.name,
                par.parameter_id,
                st.name,
                TYPE_NAME(par.user_type_id),
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

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(0);
            if (!builders.TryGetValue(objectId, out var b))
                continue;

            var systemTypeName = reader.GetString(3);
            var userTypeName = reader.IsDBNull(4) ? systemTypeName : reader.GetString(4);
            var maxLength = reader.GetInt16(5);
            var precision = reader.GetByte(6);
            var scale = reader.GetByte(7);

            var (dbType, systemType, isUnicode, isFixedLength) = MapSqlServerType(systemTypeName);
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            var paramName = reader.GetString(1);
            var paramOrdinal = reader.GetInt32(2);
            var isOutput = reader.GetBoolean(8);

            Action<ParameterBuilder> configure = p => p
                .WithName(paramName)
                .WithOrdinal(paramOrdinal)
                .WithDirection(isOutput ? Metadata.ParameterDirection.Output : Metadata.ParameterDirection.Input)
                .WithNativeTypeName(nativeTypeName)
                .WithDbType(dbType)
                .WithSystemType(systemType)
                .WithMaxLength(NormalizeMaxLength(systemTypeName, maxLength))
                .WithPrecision(HasPrecision(systemTypeName) ? precision : null)
                .WithScale(HasScale(systemTypeName) ? (int?)scale : null)
                .WithIsUnicode(isUnicode)
                .WithIsFixedLength(isFixedLength);

            if (b is StoredProcedureBuilder spb)
                spb.AddParameter(configure);
            else if (b is TableValuedFunctionBuilder tvfb)
                tvfb.AddParameter(configure);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Filtering helpers
    // ──────────────────────────────────────────────────────────────────

    private static bool ShouldIncludeSchema(string schemaName, SchemaReaderOptions options)
        => options.Schemas.Count == 0
           || options.Schemas.Contains(schemaName, StringComparer.OrdinalIgnoreCase);

    private static bool ShouldIncludeTable(string schemaName, string tableName, SchemaReaderOptions options)
        => ShouldIncludeSchema(schemaName, options)
           && (options.Tables.Count == 0 || options.Tables.Contains(tableName, StringComparer.OrdinalIgnoreCase));

    // ──────────────────────────────────────────────────────────────────
    //  Type mapping
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps a SQL Server system type name to <see cref="DbType"/>, CLR <see cref="Type"/>,
    /// and optional Unicode / fixed-length flags.
    /// </summary>
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
            "date" => (DbType.Date, typeof(DateOnly), null, null),
            "datetime" or "smalldatetime" => (DbType.DateTime, typeof(DateTime), null, null),
            "datetime2" => (DbType.DateTime2, typeof(DateTime), null, null),
            "datetimeoffset" => (DbType.DateTimeOffset, typeof(DateTimeOffset), null, null),
            "time" => (DbType.Time, typeof(TimeOnly), null, null),
            "char" => (DbType.AnsiStringFixedLength, typeof(string), false, true),
            "varchar" => (DbType.AnsiString, typeof(string), false, false),
            "text" => (DbType.AnsiString, typeof(string), false, false),
            "nchar" => (DbType.StringFixedLength, typeof(string), true, true),
            "nvarchar" => (DbType.String, typeof(string), true, false),
            "ntext" => (DbType.String, typeof(string), true, false),
            "binary" => (DbType.Binary, typeof(byte[]), null, true),
            "varbinary" or "image" => (DbType.Binary, typeof(byte[]), null, false),
            "timestamp" or "rowversion" => (DbType.Binary, typeof(byte[]), null, null),
            "uniqueidentifier" => (DbType.Guid, typeof(Guid), null, null),
            "xml" => (DbType.Xml, typeof(string), null, null),
            "sql_variant" => (DbType.Object, typeof(object), null, null),
            _ => (DbType.Object, typeof(object), null, null),
        };

    /// <summary>
    /// Constructs a human-readable native type name with facets
    /// (e.g. <c>nvarchar(256)</c>, <c>decimal(18, 2)</c>).
    /// </summary>
    private static string FormatNativeTypeName(string systemTypeName, string userTypeName, short maxLength, byte precision, byte scale)
    {
        // User-defined alias type — return the alias name as-is
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

    /// <summary>
    /// Normalizes max_length from sys.columns into character length for Unicode types,
    /// or null for types where length is not applicable.
    /// </summary>
    private static int? NormalizeMaxLength(string systemTypeName, short maxLength)
    {
        var lower = systemTypeName.ToLowerInvariant();
        return lower switch
        {
            "char" or "varchar" or "binary" or "varbinary"
                => maxLength == -1 ? null : (int?)maxLength,
            "nchar" or "nvarchar"
                => maxLength == -1 ? null : (int?)(maxLength / 2),
            _ => null,
        };
    }

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
