using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

public sealed partial class SqlServerSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadStoredProceduresAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(p.schema_id)");

        var schemaWhere = schemaFilter is not null ? $"\n    AND {schemaFilter}" : "";

        var procedures = new Dictionary<int, StoredProcedureBuilder>();

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

            using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

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
                var definition = reader.GetStringNull(definitionOrdinal);
                var description = reader.GetStringNull(descriptionOrdinal);

                var storedProcedureBuilder = new StoredProcedureBuilder()
                    .WithQualifiedName(schema, name)
                    .WithDefinition(definition)
                    .WithDescription(description);

                ApplyExtendedProperties((1, objectId, 0), storedProcedureBuilder);

                procedures[objectId] = storedProcedureBuilder;
            }
        }

        if (procedures.Count == 0)
            return;

        await ReadParametersAsync(connection, procedures, "o.type = 'P'", cancellationToken).ConfigureAwait(false);

        foreach (var (_, spb) in procedures)
            builder.AddStoredProcedure(spb.Build());
    }

    /// <inheritdoc />
    protected override async Task ReadScalarFunctionsAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(o.schema_id)");

        var schemaWhere = schemaFilter is not null ? $"\n    AND {schemaFilter}" : "";

        var functions = new Dictionary<int, ScalarFunctionBuilder>();

        var sql = $"""
            SELECT
                o.object_id,
                SCHEMA_NAME(o.schema_id)    AS schema_name,
                o.name                      AS func_name,
                m.definition,
                CAST(ep.value AS NVARCHAR(4000)) AS description
            FROM sys.objects o
            LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = o.object_id AND ep.minor_id = 0
                AND ep.class = 1 AND ep.name = 'MS_Description'
            WHERE o.type IN ('FN', 'FS') AND o.is_ms_shipped = 0{schemaWhere}
            ORDER BY SCHEMA_NAME(o.schema_id), o.name
            """;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

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
                var definition = reader.GetStringNull(definitionOrdinal);
                var description = reader.GetStringNull(descriptionOrdinal);

                var functionBuilder = new ScalarFunctionBuilder()
                    .WithQualifiedName(schema, name)
                    .WithDefinition(definition)
                    .WithDescription(description);

                ApplyExtendedProperties((1, objectId, 0), functionBuilder);

                functions[objectId] = functionBuilder;
            }
        }

        if (functions.Count == 0)
            return;

        await ReadScalarFunctionParametersAsync(connection, functions, cancellationToken).ConfigureAwait(false);

        foreach (var (_, fb) in functions)
            builder.AddScalarFunction(fb.Build());
    }

    private async Task ReadScalarFunctionParametersAsync(
        SqlConnection connection,
        Dictionary<int, ScalarFunctionBuilder> functions,
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
            WHERE o.type IN ('FN', 'FS') AND o.is_ms_shipped = 0
            ORDER BY par.object_id, par.parameter_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

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

            if (!functions.TryGetValue(objectId, out var functionBuilder))
                continue;

            var paramName = reader.GetString(nameOrdinal);
            var paramId = reader.GetInt32(paramIdOrdinal);
            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.GetStringNull(userTypeOrdinal) ?? systemTypeName;
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var isOutput = reader.GetBoolean(outputOrdinal);

            var direction = isOutput
                ? Metadata.ParameterDirection.Output
                : Metadata.ParameterDirection.Input;

            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(systemTypeName);
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            if (paramId == 0)
            {
                TypeMapping returnType = new()
                {
                    DbType = dbType,
                    NativeTypeName = nativeTypeName,
                    SystemType = systemType,
                    MaxLength = maxLengthValue,
                    Precision = precisionValue,
                    Scale = scaleValue,
                    IsUnicode = isUnicode,
                    IsFixedLength = isFixedLength,
                };
                functionBuilder.WithReturnType(returnType);
            }
            else
            {
                functionBuilder.AddParameter(parameterBuilder =>
                {
                    parameterBuilder
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
                        .WithIsFixedLength(isFixedLength);

                    ApplyExtendedProperties((2, objectId, paramId), parameterBuilder);
                    parameterBuilder.WithAnnotation(SqlServerAnnotations.SqlDbType, sqlDbType.ToString());
                });
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
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(o.schema_id)");

        var schemaWhere = schemaFilter is not null ? $"\n    AND {schemaFilter}" : "";

        var functions = new Dictionary<int, TableValuedFunctionBuilder>();

        var sql = $"""
            SELECT
                o.object_id,
                SCHEMA_NAME(o.schema_id)    AS schema_name,
                o.name                      AS func_name,
                m.definition,
                CAST(ep.value AS NVARCHAR(4000)) AS description
            FROM sys.objects o
            LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
            LEFT JOIN sys.extended_properties ep
                ON ep.major_id = o.object_id AND ep.minor_id = 0
                AND ep.class = 1 AND ep.name = 'MS_Description'
            WHERE o.type IN ('TF', 'IF', 'FT') AND o.is_ms_shipped = 0{schemaWhere}
            ORDER BY SCHEMA_NAME(o.schema_id), o.name
            """;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

            const int objectIdOrdinal = 0;
            const int schemaOrdinal = 1;
            const int nameOrdinal = 2;
            const int defOrdinal = 3;
            const int descriptionOrdinal = 4;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetInt32(objectIdOrdinal);
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var definition = reader.GetStringNull(defOrdinal);
                var description = reader.GetStringNull(descriptionOrdinal);

                var functionBuilder = new TableValuedFunctionBuilder()
                    .WithQualifiedName(schema, name)
                    .WithDefinition(definition)
                    .WithDescription(description);

                ApplyExtendedProperties((1, objectId, 0), functionBuilder);

                functions[objectId] = functionBuilder;
            }
        }

        if (functions.Count == 0)
            return;

        await ReadParametersAsync(connection, functions, "o.type IN ('TF', 'IF', 'FT')", cancellationToken).ConfigureAwait(false);

        await ReadTableValuedFunctionColumnsAsync(connection, functions, cancellationToken).ConfigureAwait(false);

        foreach (var (_, fb) in functions)
            builder.AddTableValuedFunction(fb.Build());
    }

    private async Task ReadTableValuedFunctionColumnsAsync(
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
            WHERE o.type IN ('TF', 'IF', 'FT') AND o.is_ms_shipped = 0
            ORDER BY c.object_id, c.column_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

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
            var objectId = reader.GetInt32(objectIdOrdinal);
            if (!funcs.TryGetValue(objectId, out var functionBuilder))
                continue;

            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.GetStringNull(userTypeOrdinal) ?? systemTypeName;
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var columnName = reader.GetString(columnNameOrdinal);
            var columnId = reader.GetInt32(columnIdOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);

            var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(systemTypeName);
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);
            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            ReturnColumn returnColumn = new()
            {
                Name = columnName,
                OrdinalPosition = columnId,
                IsNullable = isNullable,
                DbType = dbType,
                NativeTypeName = nativeTypeName,
                SystemType = systemType,
                MaxLength = maxLengthValue,
                Precision = precisionValue,
                Scale = scaleValue,
                IsUnicode = isUnicode,
                IsFixedLength = isFixedLength,
                Annotations = new Dictionary<string, object?>
                {
                    [SqlServerAnnotations.SqlDbType] = sqlDbType.ToString(),
                },
            };

            functionBuilder.AddReturnColumn(returnColumn);
        }
    }
}
