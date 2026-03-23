using System.Data.Common;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Provider;

/// <summary>
/// Template Method base class for <see cref="IDatabaseSchemaReader"/> implementations.
/// </summary>
/// <typeparam name="TConnection">
/// The provider-specific <see cref="DbConnection"/> type
/// (e.g., <c>SqlConnection</c>, <c>NpgsqlConnection</c>).
/// </typeparam>
/// <remarks>
/// Orchestrates the full schema-reading workflow: creates and opens a native connection
/// from the supplied connection string, reads database-level metadata, delegates to
/// provider-specific <c>Read*Async</c> methods for each object type, and assembles the
/// results via <see cref="DatabaseModelBuilder"/>. Providers override only the methods
/// relevant to their engine.
/// </remarks>
public abstract class DatabaseSchemaReader<TConnection> : IDatabaseSchemaReader
    where TConnection : DbConnection, new()
{
    /// <inheritdoc />
    public abstract string ProviderName { get; }

    /// <summary>
    /// Creates a new instance of the provider-specific connection with the given
    /// connection string.
    /// </summary>
    /// <param name="connectionString">The connection string for the target database.</param>
    /// <returns>A new, unopened <typeparamref name="TConnection"/>.</returns>
    protected virtual TConnection CreateConnection(string connectionString)
    {
        var connection = new TConnection();
        connection.ConnectionString = connectionString;
        return connection;
    }

    /// <inheritdoc />
    public async Task<DatabaseModel> ReadAsync(
        string connectionString,
        SchemaReaderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        options ??= new SchemaReaderOptions();

        await using var connection = CreateConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var builder = new DatabaseModelBuilder();

        builder
            .WithProvider(ProviderName)
            .WithDatabaseName(connection.Database);

        await ReadDatabaseMetadataAsync(connection, builder, cancellationToken).ConfigureAwait(false);

        await ReadTablesAsync(connection, builder, options, cancellationToken).ConfigureAwait(false);

        if (options.IncludeViews)
            await ReadViewsAsync(connection, builder, options, cancellationToken).ConfigureAwait(false);

        if (options.IncludeSequences)
            await ReadSequencesAsync(connection, builder, options, cancellationToken).ConfigureAwait(false);

        if (options.IncludeStoredProcedures)
            await ReadStoredProceduresAsync(connection, builder, options, cancellationToken).ConfigureAwait(false);

        if (options.IncludeScalarFunctions)
            await ReadScalarFunctionsAsync(connection, builder, options, cancellationToken).ConfigureAwait(false);

        if (options.IncludeTableValuedFunctions)
            await ReadTableValuedFunctionsAsync(connection, builder, options, cancellationToken).ConfigureAwait(false);

        if (options.IncludeUserDefinedTypes)
            await ReadUserDefinedTypesAsync(connection, builder, options, cancellationToken).ConfigureAwait(false);

        return builder.Build();
    }

    /// <summary>
    /// Reads database-level metadata such as collation, default schema, and server version.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with database-level metadata.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected abstract Task ReadDatabaseMetadataAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads all tables (with columns, indexes, keys, constraints, and triggers)
    /// from the database and adds them to the builder.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with tables.</param>
    /// <param name="options">Filtering options (schemas, table names, <see cref="SchemaReaderOptions.IncludeDefinitions"/>).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected abstract Task ReadTablesAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads all views from the database and adds them to the builder.
    /// </summary>
    /// <remarks>
    /// When <see cref="SchemaReaderOptions.IncludeDefinitions"/> is <see langword="false"/>,
    /// implementations should skip querying the SQL definition text and leave
    /// <see cref="View.Definition"/> as <see langword="null"/>.
    /// </remarks>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with views.</param>
    /// <param name="options">Filtering options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected virtual Task ReadViewsAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Reads all sequences from the database and adds them to the builder.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with sequences.</param>
    /// <param name="options">Filtering options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected virtual Task ReadSequencesAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Reads all stored procedures from the database and adds them to the builder.
    /// </summary>
    /// <remarks>
    /// When <see cref="SchemaReaderOptions.IncludeDefinitions"/> is <see langword="false"/>,
    /// implementations should skip querying the SQL definition text and leave
    /// <see cref="StoredProcedure.Definition"/> as <see langword="null"/>.
    /// </remarks>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with stored procedures.</param>
    /// <param name="options">Filtering options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected virtual Task ReadStoredProceduresAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Reads all scalar functions from the database and adds them to the builder.
    /// </summary>
    /// <remarks>
    /// When <see cref="SchemaReaderOptions.IncludeDefinitions"/> is <see langword="false"/>,
    /// implementations should skip querying the SQL definition text and leave
    /// <see cref="ScalarFunction.Definition"/> as <see langword="null"/>.
    /// </remarks>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with scalar functions.</param>
    /// <param name="options">Filtering options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected virtual Task ReadScalarFunctionsAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Reads all table-valued functions from the database and adds them to the builder.
    /// </summary>
    /// <remarks>
    /// When <see cref="SchemaReaderOptions.IncludeDefinitions"/> is <see langword="false"/>,
    /// implementations should skip querying the SQL definition text and leave
    /// <see cref="TableValuedFunction.Definition"/> as <see langword="null"/>.
    /// </remarks>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with table-valued functions.</param>
    /// <param name="options">Filtering options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected virtual Task ReadTableValuedFunctionsAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Reads all user-defined types from the database and adds them to the builder.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="builder">The builder to populate with user-defined types.</param>
    /// <param name="options">Filtering options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected virtual Task ReadUserDefinedTypesAsync(
        TConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
