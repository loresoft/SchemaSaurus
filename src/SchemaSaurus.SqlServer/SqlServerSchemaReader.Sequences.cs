using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

public sealed partial class SqlServerSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadSequencesAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // Build the WHERE clause for filtering sequences based on the specified schemas.
        var schemaFilter = BuildSchemaFilter(options.Schemas, "SCHEMA_NAME(s.schema_id)");

        // We don't need to filter out system objects here since sequences are not included in the metadata for system objects (is_ms_shipped is not a column in sys.sequences).
        var whereClause = schemaFilter is not null ? $"WHERE {schemaFilter}" : "";

        var sql = $"""
            SELECT
                s.object_id,
                s.name                          AS seq_name,
                SCHEMA_NAME(s.schema_id)        AS schema_name,
                TYPE_NAME(s.system_type_id)     AS type_name,
                CAST(s.start_value AS BIGINT)   AS start_value,
                CAST(s.increment AS BIGINT)     AS increment,
                CAST(s.minimum_value AS BIGINT) AS minimum_value,
                CAST(s.maximum_value AS BIGINT) AS maximum_value,
                s.is_cycling,
                s.cache_size,
                s.is_cached
            FROM sys.sequences s
            {whereClause}
            ORDER BY SCHEMA_NAME(s.schema_id), s.name
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int nameOrdinal = 1;
        const int schemaOrdinal = 2;
        const int typeOrdinal = 3;
        const int startOrdinal = 4;
        const int incrOrdinal = 5;
        const int minOrdinal = 6;
        const int maxOrdinal = 7;
        const int cycleOrdinal = 8;
        const int cacheOrdinal = 9;
        const int cachedOrdinal = 10;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(objectIdOrdinal);
            var seqName = reader.GetString(nameOrdinal);
            var schema = reader.GetString(schemaOrdinal);
            var typeName = reader.GetString(typeOrdinal);
            var startValue = reader.GetInt64(startOrdinal);
            var increment = reader.GetInt64(incrOrdinal);
            var minValue = reader.GetInt64(minOrdinal);
            var maxValue = reader.GetInt64(maxOrdinal);
            var isCycling = reader.GetBoolean(cycleOrdinal);
            var cacheSize = reader.GetInt32Null(cacheOrdinal);
            var cached = reader.GetBoolean(cachedOrdinal);

            // Map SQL Server system type to DbType and CLR type
            var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(typeName);

            builder.AddSequence(sequenceBuilder =>
            {
                sequenceBuilder
                    .WithQualifiedName(schema, seqName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithStartValue(startValue)
                    .WithIncrement(increment)
                    .WithMinValue(minValue)
                    .WithMaxValue(maxValue)
                    .WithIsCycling(isCycling)
                    .WithCacheSize(cacheSize);

                // Apply extended properties for the sequence (class=1, major_id=object_id, minor_id=0)
                ApplyExtendedProperties((1, objectId, 0), sequenceBuilder);
                sequenceBuilder.WithAnnotation(SqlServerAnnotations.SqlDbType, sqlDbType.ToString());
            });
        }
    }
}
