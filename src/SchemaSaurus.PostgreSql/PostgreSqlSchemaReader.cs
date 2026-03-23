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
    protected override Task ReadDatabaseMetadataAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        // TODO: Query current_database(), current_schema(), version(),
        //       pg_database.datcollate for collation
        return Task.CompletedTask;
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
        // Use options.IncludeDefinitions to control trigger definition loading.
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
        // When options.IncludeDefinitions is true, use pg_get_viewdef() for definition text.
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
        // When options.IncludeDefinitions is true, use pg_get_functiondef() for definition text.
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
        // When options.IncludeDefinitions is true, use pg_get_functiondef() for definition text.
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
        // When options.IncludeDefinitions is true, use pg_get_functiondef() for definition text.
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
