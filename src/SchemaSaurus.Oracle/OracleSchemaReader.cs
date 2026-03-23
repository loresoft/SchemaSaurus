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
    protected override Task ReadDatabaseMetadataAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        // TODO: Query V$VERSION, NLS_DATABASE_PARAMETERS for collation,
        //       SYS_CONTEXT('USERENV','CURRENT_SCHEMA') for default schema
        return Task.CompletedTask;
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
        // Use options.IncludeDefinitions to control trigger definition loading.
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
        // When options.IncludeDefinitions is true, include ALL_VIEWS.TEXT for definition.
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
        // When options.IncludeDefinitions is true, use DBMS_METADATA.GET_DDL or ALL_SOURCE.
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
        // When options.IncludeDefinitions is true, use DBMS_METADATA.GET_DDL or ALL_SOURCE.
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
