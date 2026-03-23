namespace SchemaSaurus.Metadata.Provider;

/// <summary>
/// Defines the contract for reading database structural metadata from a connection string
/// and producing an immutable <see cref="DatabaseModel"/> snapshot.
/// </summary>
/// <remarks>
/// Each supported database engine (SQL Server, PostgreSQL, MySQL, Oracle, SQLite) provides
/// its own implementation that creates and manages the appropriate native
/// <see cref="System.Data.Common.DbConnection"/> internally. Callers supply a connection
/// string and receive a fully populated model that can be serialized, compared, or visited.
/// </remarks>
public interface IDatabaseSchemaReader
{
    /// <summary>
    /// Identifies the database engine this reader targets
    /// (e.g., <c>"SqlServer"</c>, <c>"PostgreSQL"</c>, <c>"MySQL"</c>, <c>"Oracle"</c>, <c>"SQLite"</c>).
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Reads structural metadata from the database and returns an immutable <see cref="DatabaseModel"/>.
    /// </summary>
    /// <param name="connectionString">
    /// A provider-specific connection string used to connect to the target database.
    /// The implementation creates and disposes the underlying connection.
    /// </param>
    /// <param name="options">
    /// Optional filtering and inclusion options. When <see langword="null"/>, all schemas
    /// and object types are included with default settings.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A fully populated, immutable <see cref="DatabaseModel"/>.</returns>
    Task<DatabaseModel> ReadAsync(
        string connectionString,
        SchemaReaderOptions? options = null,
        CancellationToken cancellationToken = default);
}
