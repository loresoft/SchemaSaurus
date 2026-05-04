using System.Data;

using MySqlConnector;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.MySql;

/// <summary>
/// Reads structural metadata from a MySQL database using <c>INFORMATION_SCHEMA</c>.
/// </summary>
public sealed class MySqlSchemaReader : DatabaseSchemaReader<MySqlConnection>
{
    private const CommandBehavior SingleResultBehavior = CommandBehavior.SingleResult;

    /// <inheritdoc />
    public override string ProviderName => "MySQL";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@version, @@version_comment, @@collation_database";

        await using var reader = await command.ExecuteReaderAsync(SingleResultBehavior, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        builder
            .WithServerVersion(reader.GetStringNull(0))
            .WithEdition(reader.GetStringNull(1))
            .WithCollation(reader.GetStringNull(2));
    }

    /// <inheritdoc />
    protected override Task ReadTablesAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query INFORMATION_SCHEMA.TABLES, COLUMNS, STATISTICS, KEY_COLUMN_USAGE,
        //       TABLE_CONSTRAINTS, REFERENTIAL_CONSTRAINTS
        // Use options.Schemas / options.Tables for filtering.
        // Include trigger definitions.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadViewsAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query INFORMATION_SCHEMA.VIEWS, COLUMNS
        // Include VIEW_DEFINITION.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadStoredProceduresAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE', PARAMETERS
        // Include ROUTINE_DEFINITION.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadScalarFunctionsAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'FUNCTION', PARAMETERS
        // Include ROUTINE_DEFINITION.
        return Task.CompletedTask;
    }

    // Note: MySQL does not support sequences (prior to 10.3 MariaDB) or table-valued functions.
    // The base class no-op defaults apply.
}
