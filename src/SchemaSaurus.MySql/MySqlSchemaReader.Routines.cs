using System.Globalization;

using MySqlConnector;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.MySql;

public sealed partial class MySqlSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadStoredProceduresAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadRoutinesCoreAsync(connection, builder, options, "PROCEDURE", cancellationToken);
    }

    /// <inheritdoc />
    protected override Task ReadScalarFunctionsAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadRoutinesCoreAsync(connection, builder, options, "FUNCTION", cancellationToken);
    }

    private async Task ReadRoutinesCoreAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        string routineType,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "r.ROUTINE_SCHEMA");
        var schemaWhere = schemaFilter is null ? string.Empty : $"\n              AND {schemaFilter}";

        var routineLiteral = routineType.EscapeLiteral();

        var sql = $"""
            SELECT
                r.ROUTINE_SCHEMA,
                r.ROUTINE_NAME,
                r.ROUTINE_DEFINITION,
                r.DTD_IDENTIFIER,
                r.DATA_TYPE,
                r.CHARACTER_MAXIMUM_LENGTH,
                r.NUMERIC_PRECISION,
                r.NUMERIC_SCALE,
                r.IS_DETERMINISTIC,
                r.ROUTINE_COMMENT
            FROM INFORMATION_SCHEMA.ROUTINES r
            WHERE r.ROUTINE_TYPE = {routineLiteral}
              AND r.ROUTINE_SCHEMA NOT IN ('mysql', 'information_schema', 'performance_schema', 'sys'){schemaWhere}
            ORDER BY r.ROUTINE_SCHEMA, r.ROUTINE_NAME
            """;

        Dictionary<(string Schema, string Name), StoredProcedureBuilder>? storedProcedures = routineType == "PROCEDURE" ? [] : null;
        Dictionary<(string Schema, string Name), ScalarFunctionBuilder>? scalarFunctions = routineType == "FUNCTION" ? [] : null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

            const int schemaOrdinal = 0;
            const int nameOrdinal = 1;
            const int definitionOrdinal = 2;
            const int dtdOrdinal = 3;
            const int dataTypeOrdinal = 4;
            const int maxLengthOrdinal = 5;
            const int precisionOrdinal = 6;
            const int scaleOrdinal = 7;
            const int deterministicOrdinal = 8;
            const int commentOrdinal = 9;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var definition = reader.GetStringNull(definitionOrdinal);
                var nativeTypeName = reader.GetStringNull(dtdOrdinal);
                var dataType = reader.GetStringNull(dataTypeOrdinal);
                var maxLength = reader.GetValueInt32Null(maxLengthOrdinal);
                var precision = reader.GetValueInt32Null(precisionOrdinal);
                var scale = reader.GetValueInt32Null(scaleOrdinal);
                var isDeterministic = reader.GetString(deterministicOrdinal) == "YES";
                var comment = reader.GetStringNull(commentOrdinal).NullIfEmpty();

                if (routineType == "PROCEDURE")
                {
                    var procedureBuilder = new StoredProcedureBuilder()
                        .WithQualifiedName(schema, name)
                        .WithDefinition(definition)
                        .WithDescription(comment);

                    storedProcedures![(schema, name)] = procedureBuilder;
                    continue;
                }

                dataType ??= "unknown";
                nativeTypeName ??= dataType;

                var (dbType, mySqlDbType, systemType, isUnicode, isFixedLength) = MySqlTypeMapper.MapNativeType(dataType);

                TypeMapping returnType = new()
                {
                    DbType = dbType,
                    NativeTypeName = nativeTypeName,
                    SystemType = systemType,
                    MaxLength = maxLength,
                    Precision = precision.NormalizePrecision(dbType),
                    Scale = scale.NormalizeScale(dbType),
                    IsUnicode = isUnicode,
                    IsFixedLength = isFixedLength,
                };

                var functionBuilder = new ScalarFunctionBuilder()
                    .WithQualifiedName(schema, name)
                    .WithDefinition(definition)
                    .WithDescription(comment)
                    .WithReturnType(returnType)
                    .WithIsDeterministic(isDeterministic)
                    .WithAnnotation(MySqlAnnotations.MySqlDbType, mySqlDbType.ToString());

                scalarFunctions![(schema, name)] = functionBuilder;
            }
        }

        if (routineType == "PROCEDURE")
        {
            await ReadRoutineParametersAsync(connection, storedProcedures!, routineType, options, cancellationToken).ConfigureAwait(false);

            foreach (var (_, procedureBuilder) in storedProcedures!)
            {
                var procedure = procedureBuilder.Build();
                builder.AddStoredProcedure(procedure);
            }
        }
        else
        {
            await ReadRoutineParametersAsync(connection, scalarFunctions!, routineType, options, cancellationToken).ConfigureAwait(false);

            foreach (var (_, functionBuilder) in scalarFunctions!)
            {
                var function = functionBuilder.Build();
                builder.AddScalarFunction(function);
            }
        }
    }

    private async Task ReadRoutineParametersAsync<TBuilder>(
        MySqlConnection connection,
        Dictionary<(string Schema, string Name), TBuilder> routineBuilders,
        string routineType,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "p.SPECIFIC_SCHEMA");
        var schemaWhere = schemaFilter is null ? string.Empty : $"\n              AND {schemaFilter}";

        var routineLiteral = routineType.EscapeLiteral();

        var sql = $"""
            SELECT
                p.SPECIFIC_SCHEMA,
                p.SPECIFIC_NAME,
                p.PARAMETER_NAME,
                p.ORDINAL_POSITION,
                p.PARAMETER_MODE,
                p.DATA_TYPE,
                p.DTD_IDENTIFIER,
                p.CHARACTER_MAXIMUM_LENGTH,
                p.NUMERIC_PRECISION,
                p.NUMERIC_SCALE
            FROM INFORMATION_SCHEMA.PARAMETERS p
            WHERE p.ROUTINE_TYPE = {routineLiteral}
              AND p.SPECIFIC_SCHEMA NOT IN ('mysql', 'information_schema', 'performance_schema', 'sys'){schemaWhere}
              AND p.PARAMETER_NAME IS NOT NULL
            ORDER BY p.SPECIFIC_SCHEMA, p.SPECIFIC_NAME, p.ORDINAL_POSITION
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int routineOrdinal = 1;
        const int nameOrdinal = 2;
        const int ordinalOrdinal = 3;
        const int modeOrdinal = 4;
        const int dataTypeOrdinal = 5;
        const int dtdOrdinal = 6;
        const int maxLengthOrdinal = 7;
        const int precisionOrdinal = 8;
        const int scaleOrdinal = 9;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var routineName = reader.GetString(routineOrdinal);

            if (!routineBuilders.TryGetValue((schema, routineName), out var routineBuilder))
                continue;

            var parameterName = reader.GetString(nameOrdinal);
            var ordinal = Convert.ToInt32(reader.GetValue(ordinalOrdinal), CultureInfo.InvariantCulture);
            var direction = MapParameterDirection(reader.GetStringNull(modeOrdinal));
            var dataType = reader.GetStringNull(dataTypeOrdinal) ?? "unknown";
            var nativeTypeName = reader.GetStringNull(dtdOrdinal) ?? dataType;
            var maxLength = reader.GetValueInt32Null(maxLengthOrdinal);
            var precision = reader.GetValueInt32Null(precisionOrdinal);
            var scale = reader.GetValueInt32Null(scaleOrdinal);

            var (dbType, mySqlDbType, systemType, isUnicode, isFixedLength) = MySqlTypeMapper.MapNativeType(dataType);

            void Configure(ParameterBuilder parameterBuilder)
            {
                parameterBuilder
                    .WithName(parameterName)
                    .WithOrdinal(ordinal)
                    .WithDirection(direction)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLength)
                    .WithPrecision(precision)
                    .WithScale(scale)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength)
                    .WithAnnotation(MySqlAnnotations.MySqlDbType, mySqlDbType.ToString());
            }

            if (routineBuilder is StoredProcedureBuilder storedProcedureBuilder)
                storedProcedureBuilder.AddParameter(Configure);
            else if (routineBuilder is ScalarFunctionBuilder scalarFunctionBuilder)
                scalarFunctionBuilder.AddParameter(Configure);
        }
    }

    private static ParameterDirection MapParameterDirection(string? mode)
    {
        return mode?.ToUpperInvariant() switch
        {
            "OUT" => ParameterDirection.Output,
            "INOUT" => ParameterDirection.InputOutput,
            _ => ParameterDirection.Input,
        };
    }
}
