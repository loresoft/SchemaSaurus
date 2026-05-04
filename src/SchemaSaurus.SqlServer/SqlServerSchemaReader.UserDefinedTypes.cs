using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

public sealed partial class SqlServerSchemaReader
{
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

            using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

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
                // which we handle as a special case in the MapNativeType method to return appropriate metadata (e.g. DbType = Object, SystemType = typeof(object), etc.).
                var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(baseTypeName);

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
                // For other types, set to null.
                var scaleValue = HasScale(baseTypeName) ? (int?)scale : null;

                var unicode = isTableType ? null : isUnicode;
                var fixedLength = isTableType ? null : isFixedLength;

                var userTypeBuilder = new UserDefinedTypeBuilder()
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

                // Apply extended properties for the user-defined type (class=6, major_id=user_type_id, minor_id=0)
                ApplyExtendedProperties((6, userTypeId, 0), userTypeBuilder);

                udts[userTypeId] = userTypeBuilder;

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

    private async Task ReadTableTypeColumnsAsync(
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

        using var reader = await cmd.ExecuteReaderAsync(SingleResultBehavior, cancellationToken).ConfigureAwait(false);

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

            var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(systemTypeName);
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            ub.AddColumn(col =>
            {
                col
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
                    .WithIsFixedLength(isFixedLength);

                ApplyExtendedProperties((8, ttObjectId, columnId), col);
                col.WithAnnotation(SqlServerAnnotations.SqlDbType, sqlDbType.ToString());
            });
        }
    }
}
