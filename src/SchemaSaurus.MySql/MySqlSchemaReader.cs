using MySqlConnector;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.MySql;

/// <summary>
/// Reads structural metadata from a MySQL database using <c>INFORMATION_SCHEMA</c>.
/// </summary>
public sealed class MySqlSchemaReader : DatabaseSchemaReader<MySqlConnection>
{
    /// <inheritdoc />
    public override string ProviderName => "MySQL";

    /// <inheritdoc />
    protected override Task ReadDatabaseMetadataAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        // TODO: Query @@version, @@collation_database, DATABASE()
        return Task.CompletedTask;
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
        // Use options.IncludeDefinitions to control trigger definition loading.
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
        // When options.IncludeDefinitions is true, include VIEW_DEFINITION.
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
        // When options.IncludeDefinitions is true, include ROUTINE_DEFINITION.
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
        // When options.IncludeDefinitions is true, include ROUTINE_DEFINITION.
        return Task.CompletedTask;
    }

    // Note: MySQL does not support sequences (prior to 10.3 MariaDB) or table-valued functions.
    // The base class no-op defaults apply.
}
