using System.Data;

using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Oracle;

public sealed partial class OracleSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadSequencesAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "s.SEQUENCE_OWNER");

        var sql = $"""
            SELECT
                s.SEQUENCE_OWNER,
                s.SEQUENCE_NAME,
                s.MIN_VALUE,
                s.MAX_VALUE,
                s.INCREMENT_BY,
                s.CYCLE_FLAG,
                s.ORDER_FLAG,
                s.CACHE_SIZE
            FROM ALL_SEQUENCES s
            WHERE {schemaFilter}
            ORDER BY s.SEQUENCE_OWNER, s.SEQUENCE_NAME
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int nameOrdinal = 1;
        const int minOrdinal = 2;
        const int maxOrdinal = 3;
        const int incrementOrdinal = 4;
        const int cycleOrdinal = 5;
        const int orderOrdinal = 6;
        const int cacheOrdinal = 7;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var minValue = reader.GetValueInt64(minOrdinal);
            var maxValue = reader.GetValueInt64(maxOrdinal);
            var increment = reader.GetValueInt64(incrementOrdinal);
            var isCycling = reader.GetString(cycleOrdinal) == "Y";
            var isOrdered = reader.GetString(orderOrdinal) == "Y";
            var cacheSize = reader.GetValueInt32Null(cacheOrdinal);

            var startValue = increment >= 0 ? minValue : maxValue;

            builder.AddSequence(sequenceBuilder =>
            {
                sequenceBuilder
                    .WithQualifiedName(schema, name)
                    .WithDbType(DbType.Int64)
                    .WithSystemType(typeof(long))
                    .WithStartValue(startValue)
                    .WithIncrement(increment)
                    .WithMinValue(minValue)
                    .WithMaxValue(maxValue)
                    .WithIsCycling(isCycling)
                    .WithCacheSize(cacheSize)
                    .WithAnnotation(OracleAnnotations.OracleDbType, nameof(OracleDbType.Int64))
                    .WithAnnotation(OracleAnnotations.SequenceOrder, isOrdered);
            });
        }
    }
}
