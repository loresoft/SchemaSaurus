using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Oracle;

/// <summary>
/// Reads structural metadata from an Oracle database using <c>ALL_*</c> and <c>USER_*</c>
/// data dictionary views.
/// </summary>
public sealed class OracleSchemaReader : DatabaseSchemaReader<OracleConnection>
{
    /// <inheritdoc />
    public override string ProviderName => "Oracle";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') AS default_schema,
                SYS_CONTEXT('USERENV', 'SESSION_EDITION_NAME') AS edition,
                (SELECT banner_full FROM v$version WHERE banner_full LIKE 'Oracle Database%' FETCH FIRST 1 ROW ONLY) AS server_version,
                (SELECT value FROM nls_database_parameters WHERE parameter = 'NLS_CHARACTERSET') AS collation,
                (SELECT value FROM nls_database_parameters WHERE parameter = 'NLS_RDBMS_VERSION') AS compatibility_level
            FROM dual
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        builder
            .WithDefaultSchemaName(reader.IsDBNull(0) ? null : reader.GetString(0))
            .WithEdition(reader.IsDBNull(1) ? null : reader.GetString(1))
            .WithServerVersion(reader.IsDBNull(2) ? null : reader.GetString(2))
            .WithCollation(reader.IsDBNull(3) ? null : reader.GetString(3))
            .WithCompatibilityLevel(reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    /// <inheritdoc />
    protected override Task ReadTablesAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query ALL_TABLES, ALL_TAB_COLUMNS, ALL_INDEXES, ALL_IND_COLUMNS,
        //       ALL_CONSTRAINTS, ALL_CONS_COLUMNS
        // Use options.Schemas / options.Tables for filtering.
        // Include trigger definitions.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadViewsAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query ALL_VIEWS, ALL_TAB_COLUMNS
        // Include ALL_VIEWS.TEXT for definition.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadSequencesAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query ALL_SEQUENCES
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadStoredProceduresAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query ALL_PROCEDURES WHERE OBJECT_TYPE = 'PROCEDURE', ALL_ARGUMENTS
        // Use DBMS_METADATA.GET_DDL or ALL_SOURCE.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadScalarFunctionsAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query ALL_PROCEDURES WHERE OBJECT_TYPE = 'FUNCTION', ALL_ARGUMENTS
        // Use DBMS_METADATA.GET_DDL or ALL_SOURCE.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task ReadUserDefinedTypesAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Query ALL_TYPES, ALL_TYPE_ATTRS
        return Task.CompletedTask;
    }

    // Note: Oracle does not support table-valued functions in the same way as SQL Server.
    // The base class no-op default applies.
}
