using System.Data;
using System.Globalization;
using System.Text;

using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Internal;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Oracle;

/// <summary>
/// Reads structural metadata from an Oracle database using <c>ALL_*</c> and <c>USER_*</c>
/// data dictionary views.
/// </summary>
public sealed partial class OracleSchemaReader : DatabaseSchemaReader<OracleConnection>
{
    private const CommandBehavior SequentialResultBehavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;

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
                SYS_CONTEXT('USERENV', 'DB_NAME') AS database_name,
                SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') AS default_schema,
                SYS_CONTEXT('USERENV', 'SESSION_EDITION_NAME') AS edition,
                (SELECT banner_full FROM v$version WHERE banner_full LIKE 'Oracle Database%' FETCH FIRST 1 ROW ONLY) AS server_version,
                (SELECT value FROM nls_database_parameters WHERE parameter = 'NLS_CHARACTERSET') AS collation,
                (SELECT value FROM nls_database_parameters WHERE parameter = 'NLS_RDBMS_VERSION') AS compatibility_level
            FROM dual
            """;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        const int databaseNameOrdinal = 0;
        const int defaultSchemaOrdinal = 1;
        const int editionOrdinal = 2;
        const int serverVersionOrdinal = 3;
        const int collationOrdinal = 4;
        const int compatibilityOrdinal = 5;

        var databaseName = reader.GetStringNull(databaseNameOrdinal) ?? connection.DataSource;
        var defaultSchema = reader.GetStringNull(defaultSchemaOrdinal);
        var edition = reader.GetStringNull(editionOrdinal);
        var serverVersion = reader.GetStringNull(serverVersionOrdinal);
        var collation = reader.GetStringNull(collationOrdinal);
        var compatibilityLevel = reader.GetStringNull(compatibilityOrdinal);

        builder
            .WithDatabaseName(databaseName)
            .WithDefaultSchemaName(defaultSchema)
            .WithEdition(edition)
            .WithServerVersion(serverVersion)
            .WithCollation(collation)
            .WithCompatibilityLevel(compatibilityLevel);
    }

    private async Task ReadRelationColumnsAsync<TBuilder>(
        OracleConnection connection,
        Dictionary<(string Schema, string Name), TBuilder> relationBuilders,
        string relationKind,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        var relationView = relationKind == "TABLE" ? "ALL_TABLES" : "ALL_VIEWS";
        var relationColumn = relationKind == "TABLE" ? "TABLE_NAME" : "VIEW_NAME";

        var filter = BuildObjectFilter(options, "c.OWNER", "c.TABLE_NAME");

        var sql = $"""
            SELECT
                c.OWNER,
                c.TABLE_NAME,
                c.COLUMN_NAME,
                c.COLUMN_ID,
                c.DATA_TYPE,
                c.DATA_LENGTH,
                c.CHAR_LENGTH,
                c.DATA_PRECISION,
                c.DATA_SCALE,
                c.NULLABLE,
                c.IDENTITY_COLUMN,
                'NO' AS VIRTUAL_COLUMN,
                cc.COMMENTS,
                c.DATA_DEFAULT
            FROM ALL_TAB_COLUMNS c
            INNER JOIN {relationView} r ON r.OWNER = c.OWNER AND r.{relationColumn} = c.TABLE_NAME
            LEFT JOIN ALL_COL_COMMENTS cc ON cc.OWNER = c.OWNER AND cc.TABLE_NAME = c.TABLE_NAME AND cc.COLUMN_NAME = c.COLUMN_NAME
            WHERE {filter}
            ORDER BY c.OWNER, c.TABLE_NAME, c.COLUMN_ID
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int relationOrdinal = 1;
        const int nameOrdinal = 2;
        const int columnIdOrdinal = 3;
        const int dataTypeOrdinal = 4;
        const int dataLengthOrdinal = 5;
        const int charLengthOrdinal = 6;
        const int precisionOrdinal = 7;
        const int scaleOrdinal = 8;
        const int nullableOrdinal = 9;
        const int identityOrdinal = 10;
        const int virtualOrdinal = 11;
        const int commentsOrdinal = 12;
        const int defaultOrdinal = 13;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var relationName = reader.GetString(relationOrdinal);

            if (!relationBuilders.TryGetValue((schema, relationName), out var relationBuilder))
                continue;

            var columnName = reader.GetString(nameOrdinal);
            var columnId = reader.GetValueInt32(columnIdOrdinal);
            var dataType = reader.GetString(dataTypeOrdinal);
            var dataLength = reader.GetValueInt32Null(dataLengthOrdinal);
            var charLength = reader.GetValueInt32Null(charLengthOrdinal);
            var precision = reader.GetValueInt32Null(precisionOrdinal);
            var scale = reader.GetValueInt32Null(scaleOrdinal);
            var isNullable = reader.GetString(nullableOrdinal) == "Y";
            var isIdentity = reader.GetStringNull(identityOrdinal) == "YES";
            var isComputed = reader.GetStringNull(virtualOrdinal) == "YES";
            var description = reader.GetStringNull(commentsOrdinal).NullIfEmpty();
            var defaultSql = reader.GetStringNull(defaultOrdinal)?.Trim();

            var nativeTypeName = FormatNativeTypeName(dataType, dataLength, charLength, precision, scale);

            var (dbType, oracleDbType, systemType, isUnicode, isFixedLength) = MapOracleType(dataType, dataLength, precision, scale);

            var precisionValue = precision.NormalizePrecision(dbType);
            var scaleValue = scale.NormalizeScale(dbType);
            var maxLength = GetMaxLength(dataType, dataLength, charLength);

            void Configure(ColumnBuilder columnBuilder)
            {
                columnBuilder
                    .WithName(columnName)
                    .WithOrdinalPosition(columnId)
                    .WithIsNullable(isNullable)
                    .WithDefaultValueSql(isComputed ? null : defaultSql)
                    .WithIsIdentity(isIdentity)
                    .WithIsComputed(isComputed)
                    .WithComputedColumnSql(isComputed ? defaultSql : null)
                    .WithIsStored(false)
                    .WithDescription(description)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLength)
                    .WithPrecision(precisionValue)
                    .WithScale(scaleValue)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength)
                    .WithAnnotation(OracleAnnotations.OracleDbType, oracleDbType.ToString());
            }

            if (relationBuilder is TableBuilder tableBuilder)
                tableBuilder.AddColumn(Configure);
            else if (relationBuilder is ViewBuilder viewBuilder)
                viewBuilder.AddColumn(Configure);
            else if (relationBuilder is UserDefinedTypeBuilder userDefinedTypeBuilder)
                userDefinedTypeBuilder.AddColumn(Configure);
        }
    }

    private static string BuildObjectFilter(SchemaReaderOptions options, string schemaExpression, string objectExpression)
    {
        List<string> conditions = [BuildSchemaFilter(options.Schemas, schemaExpression)];

        if (options.Tables.Count > 0)
        {
            var tableFilter = BuildCaseInsensitiveFilter(options.Tables, objectExpression);
            conditions.Add(tableFilter);
        }

        return string.Join("\n              AND ", conditions);
    }

    private static string BuildSchemaFilter(IReadOnlyCollection<string> schemas, string schemaExpression)
    {
        var userMaintainedSchemaFilter = $"EXISTS (SELECT 1 FROM ALL_USERS u WHERE u.USERNAME = {schemaExpression} AND u.ORACLE_MAINTAINED = 'N')";

        if (schemas.Count == 0)
            return userMaintainedSchemaFilter;

        var list = string.Join(", ", schemas.Select(value => value.EscapeLiteral()));
        return $"{userMaintainedSchemaFilter}\n              AND {schemaExpression} IN ({list})";
    }

    private static string BuildCaseInsensitiveFilter(IReadOnlyCollection<string> values, string expression)
    {
        var capacity = 2;
        var expressionLength = expression.Length;
        foreach (var value in values)
            capacity += 21 + (expressionLength * 2) + value.Length + value.Count(character => character == '\'');

        capacity += Math.Max(0, values.Count - 1) * 4;

        var builder = StringBuilderCache.Acquire(capacity);
        builder.Append('(');

        var first = true;
        foreach (var value in values)
        {
            if (!first)
                builder.Append(" OR ");

            builder.Append($"UPPER({expression}) = UPPER({value.EscapeLiteral()})");
            first = false;
        }

        builder.Append(')');

        return StringBuilderCache.ToString(builder);
    }

    private static (DbType DbType, OracleDbType OracleDbType, Type SystemType, bool? IsUnicode, bool? IsFixedLength) MapOracleType(
        string dataType,
        int? dataLength,
        int? precision,
        int? scale)
    {
        if (dataType.Equals("RAW", StringComparison.OrdinalIgnoreCase) && dataLength == 16)
            return (DbType.Guid, OracleDbType.Raw, typeof(Guid), null, false);

        if (!dataType.Equals("NUMBER", StringComparison.OrdinalIgnoreCase) || scale.GetValueOrDefault() != 0 || precision is null)
            return OracleTypeMapper.MapNativeType(dataType);

        if (precision == 1)
            return (DbType.Boolean, OracleDbType.Boolean, typeof(bool), null, null);

        if (precision <= 5)
            return (DbType.Int16, OracleDbType.Int16, typeof(short), null, null);

        if (precision <= 10)
            return (DbType.Int32, OracleDbType.Int32, typeof(int), null, null);

        if (precision <= 19)
            return (DbType.Int64, OracleDbType.Int64, typeof(long), null, null);

        return OracleTypeMapper.MapNativeType(dataType);
    }


    private static string FormatNativeTypeName(string dataType, int? dataLength, int? charLength, int? precision, int? scale)
    {
        if (dataType.Contains("CHAR", StringComparison.OrdinalIgnoreCase) && charLength is not null)
            return $"{dataType}({charLength})";

        if (dataType.Equals("RAW", StringComparison.OrdinalIgnoreCase) && dataLength is not null)
            return $"{dataType}({dataLength})";

        if (dataType.Equals("NUMBER", StringComparison.OrdinalIgnoreCase) && precision is not null)
            return scale is null ? $"{dataType}({precision})" : $"{dataType}({precision}, {scale})";

        return dataType;
    }

    private static int? GetMaxLength(string dataType, int? dataLength, int? charLength)
    {
        if (dataType.Contains("CHAR", StringComparison.OrdinalIgnoreCase))
            return charLength;

        if (dataType.Contains("RAW", StringComparison.OrdinalIgnoreCase))
            return dataLength;

        return null;
    }

    private static ReferentialAction MapReferentialAction(string? action)
    {
        return action switch
        {
            "CASCADE" => ReferentialAction.Cascade,
            "SET NULL" => ReferentialAction.SetNull,
            "SET DEFAULT" => ReferentialAction.SetDefault,
            "RESTRICT" => ReferentialAction.Restrict,
            _ => ReferentialAction.NoAction,
        };
    }
}
