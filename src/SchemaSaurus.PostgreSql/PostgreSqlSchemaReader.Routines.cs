using Npgsql;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSQL;

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
                typ.typname,
                format_type(proc.prorettype, NULL),
                des.description
            FROM pg_proc AS proc
            JOIN pg_namespace AS ns ON ns.oid = proc.pronamespace
            JOIN pg_type AS typ ON typ.oid = proc.prorettype
            LEFT JOIN pg_description AS des ON des.objoid = proc.oid AND des.objsubid = 0
            WHERE {routineWhere}
              AND ns.nspname NOT IN ('pg_catalog', 'information_schema'){schemaWhere}
            ORDER BY ns.nspname, proc.proname
            """;

        await using var command = connection.CreateCommand();
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
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var definition = reader.GetStringNull(definitionOrdinal);
            var typeName = reader.GetString(typeOrdinal);
            var nativeTypeName = AdjustFormattedTypeName(reader.GetString(formattedTypeOrdinal));
            var description = reader.GetStringNull(descriptionOrdinal);

            if (routineKind == "p")
            {
                builder.AddStoredProcedure(storedProcedureBuilder =>
                {
                    storedProcedureBuilder
                        .WithSchemaQualifiedName(schema, name)
                        .WithDefinition(definition)
                        .WithDescription(description);
                });

                continue;
            }

            if (routineKind == "s")
            {
                var (dbType, _, systemType, _, _) = PostgreSqlTypeMapper.MapNativeType(typeName);

                builder.AddScalarFunction(functionBuilder =>
                {
                    functionBuilder
                        .WithSchemaQualifiedName(schema, name)
                        .WithDefinition(definition)
                        .WithReturnType(dbType, nativeTypeName, systemType);
                });

                continue;
            }

            builder.AddTableValuedFunction(functionBuilder =>
            {
                functionBuilder
                    .WithSchemaQualifiedName(schema, name)
                    .WithDefinition(definition);
            });
        }
    }
}
