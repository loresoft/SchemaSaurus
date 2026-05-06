using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

public sealed partial class SqlServerSchemaReader
{
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

    private async Task<Dictionary<int, ViewBuilder>> ReadViewDefinitionsAsync(
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

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

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
            var definition = reader.GetStringNull(defOrdinal);
            var description = reader.GetStringNull(descOrdinal);
            var isMaterialized = reader.GetInt32(materializedOrdinal) == 1;

            var viewBuilder = new ViewBuilder()
                .WithQualifiedName(schema, name)
                .WithDefinition(definition)
                .WithDescription(description)
                .WithIsMaterialized(isMaterialized);

            // Apply extended properties for the view itself (class=1, major_id=object_id, minor_id=0)
            ApplyExtendedProperties((1, objectId, 0), viewBuilder);

            views[objectId] = viewBuilder;
        }

        return views;
    }

    private async Task ReadViewColumnsAsync(
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
        const int collationOrdinal = 9;
        const int descOrdinal = 10;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // Filter rows based on object_id to avoid processing columns for views we're not including.
            // This is more efficient than filtering in-memory after reading all columns.
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!views.TryGetValue(objectId, out var viewBuilder))
                continue;

            var columnId = reader.GetInt32(columnIdOrdinal);
            var columnName = reader.GetString(colNameOrdinal);
            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.GetStringNull(userTypeOrdinal) ?? systemTypeName;
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);
            var collation = reader.GetStringNull(collationOrdinal);
            var description = reader.GetStringNull(descOrdinal);

            // Map SQL Server system type to DbType and CLR type, and determine Unicode/fixed-length attributes
            var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(systemTypeName);

            // Format the native type name with length/precision/scale as appropriate for the type. For user-defined types, include the user type name instead of the system type name.
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            // Normalize max length for character types (e.g. -1 for MAX) and binary types, and set to null for types where it doesn't apply
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);

            // Only set precision and scale for types where they apply (e.g. decimal, numeric, time, datetime2); set to null for other types
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;

            // Scale is applicable for decimal/numeric, and also for time/datetime2 (where it represents fractional seconds precision). For other types, set to null.
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            viewBuilder.AddColumn(columnBuilder =>
            {
                columnBuilder
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
                    .WithIsFixedLength(isFixedLength);

                // Apply extended properties for the column (class=1, major_id=object_id, minor_id=column_id)
                ApplyExtendedProperties((1, objectId, columnId), columnBuilder);
                columnBuilder.WithAnnotation(SqlServerAnnotations.SqlDbType, sqlDbType.ToString());
            });
        }
    }
}
