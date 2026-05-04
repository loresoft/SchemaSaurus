using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Sqlite;

public sealed partial class SqliteSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadViewsAsync(
        SqliteConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name, sql
            FROM sqlite_master
            WHERE type = 'view' AND instr(name, 'sqlite_') <> 1
            ORDER BY name
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int viewNameOrdinal = 0;
        const int sqlOrdinal = 1;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var viewName = reader.GetString(viewNameOrdinal);
            if (IsSpatialiteObject(viewName)
                || (options.Tables.Count > 0 && !options.Tables.Contains(viewName, StringComparer.OrdinalIgnoreCase)))
            {
                continue;
            }

            var viewSql = reader.IsDBNull(sqlOrdinal) ? null : reader.GetString(sqlOrdinal);

            var viewBuilder = new ViewBuilder()
                .WithSchemaQualifiedName(schema: null, viewName)
                .WithDefinition(viewSql);

            await ReadViewColumnsAsync(connection, viewName, viewBuilder, cancellationToken).ConfigureAwait(false);

            var view = viewBuilder.Build();
            builder.AddView(view);
        }
    }

    private static async Task ReadViewColumnsAsync(
        SqliteConnection connection,
        string viewName,
        ViewBuilder viewBuilder,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name, type, "notnull"
            FROM pragma_table_xinfo($view)
            WHERE hidden IN (0, 2, 3)
            ORDER BY cid
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$view", viewName);

        using var reader = await command.ExecuteReaderAsync(SingleResultBehavior, cancellationToken).ConfigureAwait(false);

        const int columnNameOrdinal = 0;
        const int typeNameOrdinal = 1;
        const int notNullOrdinal = 2;

        var ordinalPosition = 1;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // name | type | notnull
            var columnName = reader.GetString(columnNameOrdinal);
            var typeName = reader.IsDBNull(typeNameOrdinal) ? "" : reader.GetString(typeNameOrdinal);
            var notNull = reader.GetInt32(notNullOrdinal) != 0;

            var (dbType, systemType) = SqliteTypeMapper.MapNativeType(typeName);

            viewBuilder.AddColumn(col => col
                .WithName(columnName)
                .WithOrdinalPosition(ordinalPosition)
                .WithIsNullable(!notNull)
                .WithNativeTypeName(string.IsNullOrEmpty(typeName) ? "BLOB" : typeName)
                .WithDbType(dbType)
                .WithSystemType(systemType));

            ordinalPosition++;
        }
    }
}
