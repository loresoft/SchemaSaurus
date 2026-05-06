using System.Globalization;

using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Oracle;

public sealed partial class OracleSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadStoredProceduresAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadRoutinesCoreAsync(connection, builder, options, "PROCEDURE", cancellationToken);
    }

    /// <inheritdoc />
    protected override Task ReadScalarFunctionsAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadRoutinesCoreAsync(connection, builder, options, "FUNCTION", cancellationToken);
    }

    private async Task ReadRoutinesCoreAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        string objectType,
        CancellationToken cancellationToken)
    {
        var typeLiteral = objectType.EscapeLiteral();
        var schemaFilter = BuildSchemaFilter(options.Schemas, "o.OWNER");

        var sql = $"""
            SELECT
                o.OWNER,
                o.OBJECT_NAME,
                o.OBJECT_ID,
                LISTAGG(s.TEXT, '') WITHIN GROUP (ORDER BY s.LINE) AS DEFINITION
            FROM ALL_OBJECTS o
            LEFT JOIN ALL_SOURCE s ON s.OWNER = o.OWNER AND s.NAME = o.OBJECT_NAME AND s.TYPE = {typeLiteral}
            WHERE o.OBJECT_TYPE = {typeLiteral}
              AND {schemaFilter}
            GROUP BY o.OWNER, o.OBJECT_NAME, o.OBJECT_ID
            ORDER BY o.OWNER, o.OBJECT_NAME
            """;

        var storedProcedures = new Dictionary<(string Schema, string Name), StoredProcedureBuilder>();
        var scalarFunctions = new Dictionary<(string Schema, string Name), ScalarFunctionBuilder>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

            const int schemaOrdinal = 0;
            const int nameOrdinal = 1;
            const int definitionOrdinal = 3;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var definition = reader.GetStringNull(definitionOrdinal);

                if (objectType == "PROCEDURE")
                {
                    var storedProcedureBuilder = new StoredProcedureBuilder()
                        .WithQualifiedName(schema, name)
                        .WithDefinition(definition);

                    storedProcedures[(schema, name)] = storedProcedureBuilder;
                }
                else
                {
                    var functionBuilder = new ScalarFunctionBuilder()
                        .WithQualifiedName(schema, name)
                        .WithDefinition(definition);

                    scalarFunctions[(schema, name)] = functionBuilder;
                }
            }
        }

        if (objectType == "PROCEDURE")
        {
            await ReadRoutineArgumentsAsync(connection, storedProcedures, objectType, options, cancellationToken).ConfigureAwait(false);

            foreach (var (_, storedProcedureBuilder) in storedProcedures)
            {
                var storedProcedure = storedProcedureBuilder.Build();
                builder.AddStoredProcedure(storedProcedure);
            }
        }
        else
        {
            await ReadRoutineArgumentsAsync(connection, scalarFunctions, objectType, options, cancellationToken).ConfigureAwait(false);

            foreach (var (_, functionBuilder) in scalarFunctions)
            {
                var function = functionBuilder.Build();
                builder.AddScalarFunction(function);
            }
        }
    }

    private async Task ReadRoutineArgumentsAsync<TBuilder>(
        OracleConnection connection,
        Dictionary<(string Schema, string Name), TBuilder> routineBuilders,
        string objectType,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "a.OWNER");
        var routineFilter = BuildRoutineFilter(routineBuilders.Keys);

        var sql = $"""
            SELECT
                a.OWNER,
                a.OBJECT_NAME,
                a.ARGUMENT_NAME,
                a.POSITION,
                a.IN_OUT,
                a.DATA_TYPE,
                a.DATA_LENGTH,
                a.CHAR_LENGTH,
                a.DATA_PRECISION,
                a.DATA_SCALE
            FROM ALL_ARGUMENTS a
            WHERE {schemaFilter}
              AND {routineFilter}
            ORDER BY a.OWNER, a.OBJECT_NAME, a.POSITION, a.SEQUENCE
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int routineOrdinal = 1;
        const int nameOrdinal = 2;
        const int positionOrdinal = 3;
        const int directionOrdinal = 4;
        const int dataTypeOrdinal = 5;
        const int dataLengthOrdinal = 6;
        const int charLengthOrdinal = 7;
        const int precisionOrdinal = 8;
        const int scaleOrdinal = 9;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var routineName = reader.GetString(routineOrdinal);
            var parameterName = reader.GetStringNull(nameOrdinal);
            var position = reader.GetValueInt32(positionOrdinal);
            var direction = reader.GetStringNull(directionOrdinal);
            var dataType = reader.GetStringNull(dataTypeOrdinal) ?? "OBJECT";
            var dataLength = reader.GetValueInt32Null(dataLengthOrdinal);
            var charLength = reader.GetValueInt32Null(charLengthOrdinal);
            var precision = reader.GetValueInt32Null(precisionOrdinal);
            var scale = reader.GetValueInt32Null(scaleOrdinal);

            if (!routineBuilders.TryGetValue((schema, routineName), out var routineBuilder))
            {
                var routine = routineBuilders.FirstOrDefault(r =>
                    string.Equals(r.Key.Schema, schema, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.Key.Name, routineName, StringComparison.OrdinalIgnoreCase));

                routineBuilder = routine.Value;
                if (routineBuilder is null)
                    continue;
            }

            var nativeTypeName = FormatNativeTypeName(dataType, dataLength, charLength, precision, scale);
            var maxLength = GetMaxLength(dataType, dataLength, charLength);

            var (dbType, oracleDbType, systemType, isUnicode, isFixedLength) = MapOracleType(dataType, dataLength, precision, scale);

            var precisionValue = precision.NormalizePrecision(dbType);
            var scaleValue = scale.NormalizeScale(dbType);

            var parameterDirection = MapParameterDirection(direction);

            if (routineBuilder is ScalarFunctionBuilder functionBuilder && position == 0)
            {
                TypeMapping returnType = new()
                {
                    DbType = dbType,
                    NativeTypeName = nativeTypeName,
                    SystemType = systemType,
                    MaxLength = maxLength,
                    Precision = precisionValue,
                    Scale = scaleValue,
                    IsUnicode = isUnicode,
                    IsFixedLength = isFixedLength,
                };

                functionBuilder
                    .WithReturnType(returnType)
                    .WithAnnotation(OracleAnnotations.OracleDbType, oracleDbType.ToString());

                continue;
            }

            if (parameterName is null)
                continue;

            void Configure(ParameterBuilder parameterBuilder)
            {
                parameterBuilder
                    .WithName(parameterName)
                    .WithOrdinal(position)
                    .WithDirection(parameterDirection)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLength)
                    .WithPrecision(precisionValue)
                    .WithScale(scaleValue)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength)
                    .WithAnnotation(OracleAnnotations.OracleDbType, oracleDbType.ToString());
            }

            if (routineBuilder is StoredProcedureBuilder storedProcedureBuilder)
                storedProcedureBuilder.AddParameter(Configure);
            else if (routineBuilder is ScalarFunctionBuilder scalarFunctionBuilder)
                scalarFunctionBuilder.AddParameter(Configure);
        }
    }

    private static string BuildRoutineFilter(IEnumerable<(string Schema, string Name)> routines)
    {
        var filters = routines
            .Select(r => $"(UPPER(a.OWNER) = UPPER({r.Schema.EscapeLiteral()}) AND UPPER(a.OBJECT_NAME) = UPPER({r.Name.EscapeLiteral()}))");

        return $"({string.Join(" OR ", filters)})";
    }

    private static ParameterDirection MapParameterDirection(string? direction)
    {
        return direction switch
        {
            "OUT" => ParameterDirection.Output,
            "IN/OUT" => ParameterDirection.InputOutput,
            _ => ParameterDirection.Input,
        };
    }
}
