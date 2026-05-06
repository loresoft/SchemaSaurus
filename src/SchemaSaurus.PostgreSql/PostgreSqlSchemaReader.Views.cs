using Npgsql;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSql;

public sealed partial class PostgreSqlSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadViewsAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return ReadViewsCoreAsync(connection, builder, options, cancellationToken);
    }

    private async Task ReadViewsCoreAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var viewFilter = BuildTableFilter(options, "ns.nspname", "cls.relname");

        var views = new Dictionary<uint, ViewBuilder>();

        var sql = $"""
            SELECT
                cls.oid,
                ns.nspname,
                cls.relname,
                cls.relkind,
                pg_get_viewdef(cls.oid, true),
                des.description
            FROM pg_class AS cls
            JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
            LEFT JOIN pg_description AS des ON des.objoid = cls.oid AND des.objsubid = 0
            WHERE cls.relkind IN ('v', 'm')
              AND {viewFilter}
              AND NOT EXISTS (
                  SELECT 1
                  FROM pg_depend dep
                  WHERE dep.objid = cls.oid AND dep.deptype IN ('e', 'x')
              )
            ORDER BY ns.nspname, cls.relname
            """;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

            const int objectIdOrdinal = 0;
            const int schemaOrdinal = 1;
            const int nameOrdinal = 2;
            const int kindOrdinal = 3;
            const int definitionOrdinal = 4;
            const int descriptionOrdinal = 5;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var objectId = reader.GetFieldValue<uint>(objectIdOrdinal);
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var kind = reader.GetChar(kindOrdinal);
                var definition = reader.GetStringNull(definitionOrdinal);
                var description = reader.GetStringNull(descriptionOrdinal);

                var viewBuilder = new ViewBuilder()
                    .WithQualifiedName(schema, name)
                    .WithDefinition(definition)
                    .WithDescription(description)
                    .WithIsMaterialized(kind == 'm');

                views[objectId] = viewBuilder;
            }
        }

        if (views.Count == 0)
            return;

        await ReadRelationColumnsAsync(connection, views, viewFilter, cancellationToken).ConfigureAwait(false);

        foreach (var (_, viewBuilder) in views)
        {
            var view = viewBuilder.Build();
            builder.AddView(view);
        }
    }
}
