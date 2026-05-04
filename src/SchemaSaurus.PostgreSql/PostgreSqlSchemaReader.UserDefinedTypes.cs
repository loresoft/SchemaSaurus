using Npgsql;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSql;

public sealed partial class PostgreSqlSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadUserDefinedTypesAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadUserDefinedTypesCoreAsync(connection, builder, options, cancellationToken);
    }

    private async Task ReadUserDefinedTypesCoreAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "ns.nspname");

        var schemaWhere = schemaFilter is null ? "" : $"\n              AND {schemaFilter}";

        var sql = $"""
            SELECT
                typ.oid,
                ns.nspname,
                typ.typname,
                typ.typtype::text,
                basetyp.typname,
                format_type(basetyp.oid, typ.typtypmod),
                array_remove(array_agg(enum.enumlabel ORDER BY enum.enumsortorder), NULL) AS labels
            FROM pg_type AS typ
            JOIN pg_namespace AS ns ON ns.oid = typ.typnamespace
            LEFT JOIN pg_type AS basetyp ON basetyp.oid = typ.typbasetype
            LEFT JOIN pg_enum AS enum ON enum.enumtypid = typ.oid
            LEFT JOIN pg_class AS typecls ON typecls.oid = typ.typrelid
            WHERE ((typ.typtype = 'c' AND typecls.relkind = 'c') OR typ.typtype IN ('e', 'd'))
              AND ns.nspname NOT IN ('pg_catalog', 'information_schema'){schemaWhere}
            GROUP BY typ.oid, ns.nspname, typ.typname, typ.typtype, basetyp.typname, basetyp.oid, typ.typtypmod
            ORDER BY ns.nspname, typ.typname
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int typeIdOrdinal = 0;
        const int schemaOrdinal = 1;
        const int nameOrdinal = 2;
        const int kindOrdinal = 3;
        const int baseTypeOrdinal = 4;
        const int nativeTypeOrdinal = 5;
        const int labelsOrdinal = 6;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            _ = reader.GetValue(typeIdOrdinal);
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var kindCode = reader.GetString(kindOrdinal);
            var baseTypeName = reader.GetStringNull(baseTypeOrdinal) ?? "record";
            var formattedTypeName = reader.GetStringNull(nativeTypeOrdinal);
            var labels = reader.GetFieldValueNull<string[]>(labelsOrdinal) ?? [];

            var kind = MapUserDefinedTypeKind(kindCode);
            var nativeTypeName = formattedTypeName is null ? name : AdjustFormattedTypeName(formattedTypeName);
            var (dbType, npgsqlDbType, systemType, isUnicode, isFixedLength) = PostgreSqlTypeMapper.MapNativeType(baseTypeName);

            builder.AddUserDefinedType(userDefinedTypeBuilder =>
            {
                userDefinedTypeBuilder
                    .WithSchemaQualifiedName(schema, name)
                    .WithKind(kind)
                    .WithDbType(dbType)
                    .WithNativeTypeName(nativeTypeName)
                    .WithSystemType(systemType)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength)
                    .WithAnnotation(PostgreSqlAnnotations.NpgsqlDbType, npgsqlDbType.ToString());

                foreach (var label in labels)
                    userDefinedTypeBuilder.AddEnumLabel(label);
            });
        }
    }
}
