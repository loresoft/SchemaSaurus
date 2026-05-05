using MySqlConnector;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.MySql;

public sealed partial class MySqlSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadViewsAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var views = await ReadViewDefinitionsAsync(connection, options, cancellationToken).ConfigureAwait(false);
        if (views.Count == 0)
            return;

        await ReadRelationColumnsAsync(connection, views, "= 'VIEW'", options, cancellationToken).ConfigureAwait(false);

        foreach (var (_, viewBuilder) in views)
        {
            var view = viewBuilder.Build();
            builder.AddView(view);
        }
    }

    private static async Task<Dictionary<(string Schema, string Name), ViewBuilder>> ReadViewDefinitionsAsync(
        MySqlConnection connection,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var tableFilter = BuildInformationSchemaTableFilter(options, "v.TABLE_SCHEMA", "v.TABLE_NAME");

        var sql = $"""
            SELECT
                v.TABLE_SCHEMA,
                v.TABLE_NAME,
                v.VIEW_DEFINITION,
                CASE WHEN t.TABLE_COMMENT = 'VIEW' THEN NULL ELSE t.TABLE_COMMENT END AS TABLE_COMMENT
            FROM INFORMATION_SCHEMA.VIEWS v
            INNER JOIN INFORMATION_SCHEMA.TABLES t
                ON t.TABLE_SCHEMA = v.TABLE_SCHEMA AND t.TABLE_NAME = v.TABLE_NAME
            WHERE {tableFilter}
            ORDER BY v.TABLE_SCHEMA, v.TABLE_NAME
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int nameOrdinal = 1;
        const int definitionOrdinal = 2;
        const int commentOrdinal = 3;

        var views = new Dictionary<(string Schema, string Name), ViewBuilder>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var name = reader.GetString(nameOrdinal);
            var definition = reader.GetStringNull(definitionOrdinal);
            var comment = reader.GetStringNull(commentOrdinal).NullIfEmpty();

            var viewBuilder = new ViewBuilder()
                .WithSchemaQualifiedName(schema, name)
                .WithDefinition(definition)
                .WithDescription(comment);

            views[(schema, name)] = viewBuilder;
        }

        return views;
    }
}
