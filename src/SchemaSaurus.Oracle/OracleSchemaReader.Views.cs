using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Oracle;

public sealed partial class OracleSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadViewsAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var views = await ReadViewDefinitionsAsync(connection, options, cancellationToken).ConfigureAwait(false);
        if (views.Count == 0)
            return;

        await ReadRelationColumnsAsync(connection, views, "VIEW", options, cancellationToken).ConfigureAwait(false);

        foreach (var (_, viewBuilder) in views)
        {
            var view = viewBuilder.Build();
            builder.AddView(view);
        }
    }

    private static async Task<Dictionary<(string Schema, string Name), ViewBuilder>> ReadViewDefinitionsAsync(
        OracleConnection connection,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var filter = BuildObjectFilter(options, "v.OWNER", "v.VIEW_NAME");

        var sql = $"""
            SELECT
                v.OWNER,
                v.VIEW_NAME,
                vc.COMMENTS,
                DBMS_METADATA.GET_DDL('VIEW', v.VIEW_NAME, v.OWNER) AS DEFINITION
            FROM ALL_VIEWS v
            LEFT JOIN ALL_TAB_COMMENTS vc ON vc.OWNER = v.OWNER AND vc.TABLE_NAME = v.VIEW_NAME
            WHERE {filter}
            ORDER BY v.OWNER, v.VIEW_NAME
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int nameOrdinal = 1;
        const int commentsOrdinal = 2;
        const int definitionOrdinal = 3;

        var views = new Dictionary<(string Schema, string Name), ViewBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var description = reader.GetStringNull(commentsOrdinal).NullIfEmpty();
            var definition = reader.GetStringNull(definitionOrdinal);

            var viewBuilder = new ViewBuilder()
                .WithSchemaQualifiedName(schema, name)
                .WithDescription(description)
                .WithDefinition(definition);

            views[(schema, name)] = viewBuilder;
        }

        return views;
    }
}
