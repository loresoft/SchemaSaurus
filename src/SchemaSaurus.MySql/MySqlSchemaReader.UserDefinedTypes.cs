using MySqlConnector;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.MySql;

public sealed partial class MySqlSchemaReader
{
    /// <inheritdoc />
    protected override Task ReadUserDefinedTypesAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
