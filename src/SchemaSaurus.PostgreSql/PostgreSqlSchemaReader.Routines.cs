using Npgsql;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSql;

public sealed partial class PostgreSqlSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadStoredProceduresAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadRoutinesCoreAsync(connection, builder, options, "p", cancellationToken);
    }

    /// <inheritdoc />
    protected override Task ReadScalarFunctionsAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadRoutinesCoreAsync(connection, builder, options, "s", cancellationToken);
    }

    /// <inheritdoc />
    protected override Task ReadTableValuedFunctionsAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadRoutinesCoreAsync(connection, builder, options, "t", cancellationToken);
    }

    private async Task ReadRoutinesCoreAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        string routineKind,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "ns.nspname");
        var schemaWhere = schemaFilter is null ? "" : $"\n              AND {schemaFilter}";

        var routineWhere = routineKind switch
        {
            "p" => "proc.prokind = 'p'",
            "s" => "proc.prokind = 'f' AND NOT proc.proretset",
            _ => "proc.prokind = 'f' AND proc.proretset",
        };

        var sql = $"""
            SELECT
                proc.oid,
                ns.nspname,
                proc.proname,
                pg_get_functiondef(proc.oid),
                COALESCE(base_typ.typname, typ.typname) AS system_type_name,
                format_type(proc.prorettype, NULL),
                des.description
            FROM pg_proc AS proc
            JOIN pg_namespace AS ns ON ns.oid = proc.pronamespace
            JOIN pg_type AS typ ON typ.oid = proc.prorettype
            LEFT JOIN pg_type AS base_typ ON base_typ.oid = typ.typbasetype
            LEFT JOIN pg_description AS des ON des.objoid = proc.oid AND des.objsubid = 0
            WHERE {routineWhere}
              AND ns.nspname NOT IN ('pg_catalog', 'information_schema'){schemaWhere}
            ORDER BY ns.nspname, proc.proname
            """;

        Dictionary<uint, StoredProcedureBuilder>? storedProcedures = routineKind == "p" ? [] : null;
        Dictionary<uint, ScalarFunctionBuilder>? scalarFunctions = routineKind == "s" ? [] : null;
        Dictionary<uint, TableValuedFunctionBuilder>? tableValuedFunctions = routineKind == "t" ? [] : null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

            const int schemaOrdinal = 1;
            const int nameOrdinal = 2;
            const int definitionOrdinal = 3;
            const int typeOrdinal = 4;
            const int formattedTypeOrdinal = 5;
            const int descriptionOrdinal = 6;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetFieldValue<uint>(0);
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var definition = reader.GetStringNull(definitionOrdinal);
                var typeName = reader.GetString(typeOrdinal);
                var nativeType = reader.GetString(formattedTypeOrdinal);
                var description = reader.GetStringNull(descriptionOrdinal);

                var nativeTypeName = AdjustFormattedTypeName(nativeType);

                if (routineKind == "p")
                {
                    var storedProcedureBuilder = new StoredProcedureBuilder()
                        .WithSchemaQualifiedName(schema, name)
                        .WithDefinition(definition)
                        .WithDescription(description);

                    storedProcedures![objectId] = storedProcedureBuilder;

                    continue;
                }

                if (routineKind == "s")
                {
                    var (dbType, _, systemType, _, _) = PostgreSqlTypeMapper.MapNativeType(typeName);

                    var functionBuilder = new ScalarFunctionBuilder()
                        .WithSchemaQualifiedName(schema, name)
                        .WithDefinition(definition)
                        .WithDescription(description)
                        .WithReturnType(dbType, nativeTypeName, systemType);

                    scalarFunctions![objectId] = functionBuilder;

                    continue;
                }

                var tableValuedFunctionBuilder = new TableValuedFunctionBuilder()
                    .WithSchemaQualifiedName(schema, name)
                    .WithDefinition(definition)
                    .WithDescription(description);

                tableValuedFunctions![objectId] = tableValuedFunctionBuilder;
            }
        }

        if (routineKind == "p")
        {
            await ReadRoutineParametersAsync(connection, storedProcedures!, routineKind, options, cancellationToken).ConfigureAwait(false);

            foreach (var (_, storedProcedureBuilder) in storedProcedures!)
                builder.AddStoredProcedure(storedProcedureBuilder.Build());
        }
        else if (routineKind == "s")
        {
            await ReadRoutineParametersAsync(connection, scalarFunctions!, routineKind, options, cancellationToken).ConfigureAwait(false);

            foreach (var (_, functionBuilder) in scalarFunctions!)
                builder.AddScalarFunction(functionBuilder.Build());
        }
        else
        {
            await ReadRoutineParametersAsync(connection, tableValuedFunctions!, routineKind, options, cancellationToken).ConfigureAwait(false);

            foreach (var (_, functionBuilder) in tableValuedFunctions!)
                builder.AddTableValuedFunction(functionBuilder.Build());
        }
    }

    private async Task ReadRoutineParametersAsync<TBuilder>(
        NpgsqlConnection connection,
        Dictionary<uint, TBuilder> routineBuilders,
        string routineKind,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        if (routineBuilders.Count == 0)
            return;

        var schemaFilter = BuildSchemaFilter(options.Schemas, "ns.nspname");
        var schemaWhere = schemaFilter is null ? "" : $"\n              AND {schemaFilter}";

        var routineWhere = routineKind switch
        {
            "p" => "proc.prokind = 'p'",
            "s" => "proc.prokind = 'f' AND NOT proc.proretset",
            _ => "proc.prokind = 'f' AND proc.proretset",
        };

        var sql = $"""
            SELECT
                proc.oid,
                param.ordinal_position,
                param.parameter_name,
                param.parameter_mode::text,
                COALESCE(base_typ.typname, typ.typname) AS system_type_name,
                format_type(param.type_oid, NULL) AS formatted_type_name,
                format_type(base_typ.oid, typ.typtypmod) AS formatted_base_type_name
            FROM pg_proc AS proc
            JOIN pg_namespace AS ns ON ns.oid = proc.pronamespace
            CROSS JOIN LATERAL unnest(
                COALESCE(proc.proallargtypes, proc.proargtypes::oid[]),
                COALESCE(proc.proargmodes, array_fill('i'::"char", ARRAY[proc.pronargs])),
                COALESCE(proc.proargnames, array_fill(NULL::text, ARRAY[COALESCE(array_length(proc.proallargtypes, 1), proc.pronargs)]))
            ) WITH ORDINALITY AS param(type_oid, parameter_mode, parameter_name, ordinal_position)
            JOIN pg_type AS typ ON typ.oid = param.type_oid
            LEFT JOIN pg_type AS base_typ ON base_typ.oid = typ.typbasetype
            WHERE {routineWhere}
              AND ns.nspname NOT IN ('pg_catalog', 'information_schema'){schemaWhere}
              AND param.parameter_name IS NOT NULL
              AND param.parameter_mode <> 't'
            ORDER BY proc.oid, param.ordinal_position
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int positionOrdinal = 1;
        const int nameOrdinal = 2;
        const int directionOrdinal = 3;
        const int typeNameOrdinal = 4;
        const int formattedTypeOrdinal = 5;
        const int formattedBaseTypeOrdinal = 6;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetFieldValue<uint>(objectIdOrdinal);
            if (!routineBuilders.TryGetValue(objectId, out var routineBuilder))
                continue;

            var ordinal = (int)reader.GetInt64(positionOrdinal);
            var parameterName = reader.GetString(nameOrdinal);
            var direction = MapParameterDirection(reader.GetString(directionOrdinal));
            var typeName = reader.GetString(typeNameOrdinal);
            var formattedTypeName = AdjustFormattedTypeName(reader.GetString(formattedTypeOrdinal));
            var formattedBaseTypeName = reader.GetStringNull(formattedBaseTypeOrdinal);

            var nativeTypeName = formattedBaseTypeName is null
                ? formattedTypeName
                : AdjustFormattedTypeName(formattedBaseTypeName);

            var (dbType, npgsqlDbType, systemType, isUnicode, isFixedLength) = PostgreSqlTypeMapper.MapNativeType(typeName);
            var maxLength = GetMaxLength(nativeTypeName);
            var precision = GetPrecision(nativeTypeName);
            var scale = GetScale(nativeTypeName);

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
                    .WithAnnotation(PostgreSqlAnnotations.NpgsqlDbType, npgsqlDbType.ToString());
            }

            if (routineBuilder is StoredProcedureBuilder storedProcedureBuilder)
                storedProcedureBuilder.AddParameter(Configure);
            else if (routineBuilder is ScalarFunctionBuilder scalarFunctionBuilder)
                scalarFunctionBuilder.AddParameter(Configure);
            else if (routineBuilder is TableValuedFunctionBuilder tableValuedFunctionBuilder)
                tableValuedFunctionBuilder.AddParameter(Configure);
        }
    }

    private static ParameterDirection MapParameterDirection(string direction)
    {
        return direction switch
        {
            "o" => ParameterDirection.Output,
            "b" => ParameterDirection.InputOutput,
            _ => ParameterDirection.Input,
        };
    }
}
