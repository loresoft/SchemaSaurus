using Npgsql;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSQL;

public sealed partial class PostgreSqlSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadSequencesAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadSequencesCoreAsync(connection, builder, options, cancellationToken);
    }

    private async Task ReadSequencesCoreAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "ns.nspname");

        var schemaWhere = schemaFilter is null ? "" : $"\n              AND {schemaFilter}";

        var sql = $"""
            SELECT
                ns.nspname,
                cls.relname,
                typ.typname,
                seq.seqstart,
                seq.seqincrement,
                seq.seqmin,
                seq.seqmax,
                seq.seqcycle,
                seq.seqcache
            FROM pg_sequence AS seq
            JOIN pg_class AS cls ON cls.oid = seq.seqrelid
            JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
            JOIN pg_type AS typ ON typ.oid = seq.seqtypid
            WHERE NOT EXISTS (
                SELECT 1
                FROM pg_depend AS dep
                WHERE dep.objid = cls.oid AND dep.deptype IN ('i', 'I', 'a')
            ){schemaWhere}
            ORDER BY ns.nspname, cls.relname
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int nameOrdinal = 1;
        const int typeOrdinal = 2;
        const int startOrdinal = 3;
        const int incrementOrdinal = 4;
        const int minOrdinal = 5;
        const int maxOrdinal = 6;
        const int cycleOrdinal = 7;
        const int cacheOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var typeName = reader.GetString(typeOrdinal);
            var startValue = reader.GetInt64(startOrdinal);
            var increment = reader.GetInt64(incrementOrdinal);
            var minValue = reader.GetInt64(minOrdinal);
            var maxValue = reader.GetInt64(maxOrdinal);
            var isCycling = reader.GetBoolean(cycleOrdinal);
            var cacheSize = reader.GetInt64(cacheOrdinal);

            typeName = NormalizeSequenceTypeName(typeName);

            var (dbType, npgsqlDbType, systemType, _, _) = PostgreSqlTypeMapper.MapNativeType(typeName);
            var cacheSizeValue = cacheSize > int.MaxValue ? null : (int?)cacheSize;

            builder.AddSequence(sequenceBuilder =>
            {
                sequenceBuilder
                    .WithSchemaQualifiedName(schema, name)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithStartValue(startValue)
                    .WithIncrement(increment)
                    .WithMinValue(minValue)
                    .WithMaxValue(maxValue)
                    .WithIsCycling(isCycling)
                    .WithCacheSize(cacheSizeValue)
                    .WithAnnotation(PostgreSqlAnnotations.NpgsqlDbType, npgsqlDbType.ToString());
            });
        }
    }
}
