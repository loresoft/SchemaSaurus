using Npgsql;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSQL;

/// <summary>
/// Reads structural metadata from a PostgreSQL database using <c>pg_catalog</c>
/// and <c>information_schema</c>.
/// </summary>
public sealed class PostgreSqlSchemaReader : DatabaseSchemaReader<NpgsqlConnection>
{
    /// <inheritdoc />
    public override string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                current_schema(),
                version(),
                d.datcollate,
                current_setting('server_version_num', true)
            FROM pg_database d
            WHERE d.datname = current_database()
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        builder
            .WithDefaultSchemaName(reader.IsDBNull(0) ? null : reader.GetString(0))
            .WithServerVersion(reader.IsDBNull(1) ? null : reader.GetString(1))
            .WithCollation(reader.IsDBNull(2) ? null : reader.GetString(2))
            .WithEdition("PostgreSQL")
            .WithCompatibilityLevel(reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    /// <inheritdoc />
    protected override Task ReadTablesAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query pg_catalog.pg_class (relkind = 'r'), pg_attribute, pg_index,
        //       pg_constraint, information_schema.columns
        // Use options.Schemas / options.Tables for filtering.
        // Include trigger definitions.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadViewsAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query pg_catalog.pg_class (relkind IN ('v','m')), pg_attribute
        // Use pg_get_viewdef() for definition text.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadSequencesAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query pg_catalog.pg_class (relkind = 'S'), pg_sequences
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadStoredProceduresAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query pg_catalog.pg_proc WHERE prokind = 'p'
        // Use pg_get_functiondef() for definition text.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadScalarFunctionsAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query pg_catalog.pg_proc WHERE prokind = 'f' AND proretset = false
        // Use pg_get_functiondef() for definition text.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadTableValuedFunctionsAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query pg_catalog.pg_proc WHERE prokind = 'f' AND proretset = true
        // Use pg_get_functiondef() for definition text.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadUserDefinedTypesAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query pg_catalog.pg_type WHERE typtype IN ('c','e','d')
        return Task.CompletedTask;
    }
}
