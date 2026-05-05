using System.Data;
using System.Globalization;

using MySqlConnector;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.MySql;

/// <summary>
/// Reads structural metadata from a MySQL database using <c>INFORMATION_SCHEMA</c>.
/// </summary>
public sealed partial class MySqlSchemaReader : DatabaseSchemaReader<MySqlConnection>
{
    private const CommandBehavior SequentialResultBehavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;

    /// <inheritdoc />
    public override string ProviderName => "MySQL";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        MySqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@version, @@version_comment, @@collation_database, DATABASE(), @@version_compile_machine";

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        const int versionOrdinal = 0;
        const int commentOrdinal = 1;
        const int collationOrdinal = 2;
        const int schemaOrdinal = 3;
        const int engineEditionOrdinal = 4;

        builder
            .WithServerVersion(reader.GetStringNull(versionOrdinal))
            .WithEdition(reader.GetStringNull(commentOrdinal))
            .WithCollation(reader.GetStringNull(collationOrdinal))
            .WithDefaultSchemaName(reader.GetStringNull(schemaOrdinal))
            .WithEngineEdition(reader.GetStringNull(engineEditionOrdinal));
    }

    private async Task ReadRelationColumnsAsync<TBuilder>(
        MySqlConnection connection,
        Dictionary<(string Schema, string Name), TBuilder> relationBuilders,
        string tableTypeFilter,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        var tableFilter = BuildInformationSchemaTableFilter(options, "c.TABLE_SCHEMA", "c.TABLE_NAME");

        var sql = $"""
            SELECT
                c.TABLE_SCHEMA,
                c.TABLE_NAME,
                c.COLUMN_NAME,
                c.ORDINAL_POSITION,
                c.COLUMN_DEFAULT,
                CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IS_NULLABLE,
                c.DATA_TYPE,
                c.COLUMN_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.NUMERIC_PRECISION,
                c.NUMERIC_SCALE,
                c.CHARACTER_SET_NAME,
                c.COLLATION_NAME,
                c.COLUMN_COMMENT,
                c.EXTRA,
                CASE WHEN c.EXTRA LIKE '%auto_increment%' THEN 1 ELSE 0 END AS IS_IDENTITY,
                CASE WHEN c.EXTRA LIKE '%GENERATED%' THEN 1 ELSE 0 END AS IS_GENERATED,
                c.GENERATION_EXPRESSION
            FROM INFORMATION_SCHEMA.COLUMNS c
            INNER JOIN INFORMATION_SCHEMA.TABLES t
                ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
            WHERE t.TABLE_TYPE {tableTypeFilter}
              AND {tableFilter}
            ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int tableOrdinal = 1;
        const int nameOrdinal = 2;
        const int ordinalPositionOrdinal = 3;
        const int defaultOrdinal = 4;
        const int nullableOrdinal = 5;
        const int dataTypeOrdinal = 6;
        const int columnTypeOrdinal = 7;
        const int maxLengthOrdinal = 8;
        const int precisionOrdinal = 9;
        const int scaleOrdinal = 10;
        const int characterSetOrdinal = 11;
        const int collationOrdinal = 12;
        const int commentOrdinal = 13;
        const int extraOrdinal = 14;
        const int identityOrdinal = 15;
        const int generatedOrdinal = 16;
        const int generationExpressionOrdinal = 17;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var tableName = reader.GetString(tableOrdinal);
            if (!relationBuilders.TryGetValue((schema, tableName), out var relationBuilder))
                continue;

            var columnName = reader.GetString(nameOrdinal);
            var ordinalPosition = Convert.ToInt32(reader.GetValue(ordinalPositionOrdinal), CultureInfo.InvariantCulture);
            var defaultSql = reader.GetStringNull(defaultOrdinal);
            var isNullable = reader.GetInt32(nullableOrdinal) == 1;
            var dataType = reader.GetString(dataTypeOrdinal);
            var nativeTypeName = reader.GetString(columnTypeOrdinal);
            var maxLength = GetInt32Null(reader.GetValueNull(maxLengthOrdinal));
            var precision = GetInt32Null(reader.GetValueNull(precisionOrdinal));
            var scale = GetInt32Null(reader.GetValueNull(scaleOrdinal));
            var characterSet = reader.GetStringNull(characterSetOrdinal);
            var collation = reader.GetStringNull(collationOrdinal);
            var comment = NullIfEmpty(reader.GetStringNull(commentOrdinal));
            var extra = reader.GetStringNull(extraOrdinal) ?? string.Empty;
            var isIdentity = reader.GetInt32(identityOrdinal) == 1;
            var isGenerated = reader.GetInt32(generatedOrdinal) == 1;
            var generationExpression = reader.GetStringNull(generationExpressionOrdinal);
            var isStored = extra.Contains("STORED", StringComparison.OrdinalIgnoreCase);

            var (dbType, mySqlDbType, systemType, isUnicode, isFixedLength) = MySqlTypeMapper.MapNativeType(dataType);

            if (dbType != DbType.Decimal)
            {
                precision = null;
                scale = null;
            }

            if (isUnicode is not null && characterSet is not null)
                isUnicode = IsUnicodeCharacterSet(characterSet);

            if (dataType.Equals("tinyint", StringComparison.OrdinalIgnoreCase)
                && nativeTypeName.StartsWith("tinyint(1)", StringComparison.OrdinalIgnoreCase))
            {
                dbType = DbType.Boolean;
                mySqlDbType = MySqlDbType.Bool;
                systemType = typeof(bool);
            }

            void Configure(ColumnBuilder columnBuilder)
            {
                columnBuilder
                    .WithName(columnName)
                    .WithOrdinalPosition(ordinalPosition)
                    .WithIsNullable(isNullable)
                    .WithDefaultValueSql(isGenerated ? null : defaultSql)
                    .WithIsIdentity(isIdentity)
                    .WithIdentitySeed(isIdentity ? 1 : null)
                    .WithIdentityIncrement(isIdentity ? 1 : null)
                    .WithIsComputed(isGenerated)
                    .WithComputedColumnSql(isGenerated ? generationExpression : null)
                    .WithIsStored(isGenerated && isStored)
                    .WithCollation(collation)
                    .WithDescription(comment)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLength)
                    .WithPrecision(precision)
                    .WithScale(scale)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength)
                    .WithAnnotation(MySqlAnnotations.MySqlDbType, mySqlDbType.ToString())
                    .WithAnnotation(MySqlAnnotations.CharacterSet, characterSet);
            }

            if (relationBuilder is TableBuilder tableBuilder)
                tableBuilder.AddColumn(Configure);
            else if (relationBuilder is ViewBuilder viewBuilder)
                viewBuilder.AddColumn(Configure);
        }
    }

    private static string BuildInformationSchemaTableFilter(SchemaReaderOptions options, string schemaExpression, string tableExpression)
    {
        List<string> conditions = [$"{schemaExpression} NOT IN ('mysql', 'information_schema', 'performance_schema', 'sys')"];

        var schemaFilter = BuildSchemaFilter(options.Schemas, schemaExpression);
        if (schemaFilter is not null)
            conditions.Add(schemaFilter);

        if (options.Tables.Count > 0)
        {
            var list = string.Join(", ", options.Tables.Select(EscapeLiteral));
            conditions.Add($"{tableExpression} IN ({list})");
        }

        return string.Join("\n              AND ", conditions);
    }

    private static string? BuildSchemaFilter(IReadOnlyCollection<string> schemas, string schemaExpression)
    {
        if (schemas.Count == 0)
            return null;

        var list = string.Join(", ", schemas.Select(EscapeLiteral));
        return $"{schemaExpression} IN ({list})";
    }

    private static string EscapeLiteral(string value)
    {
        return $"'{value.Replace("'", "''")}'";
    }

    private static int? GetInt32Null(object? value)
    {
        if (value is null or DBNull)
            return null;

        var longValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
        return longValue > int.MaxValue ? null : (int)longValue;
    }

    private static long? GetInt64Null(object? value)
    {
        return value is null or DBNull ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool IsUnicodeCharacterSet(string characterSet)
    {
        return characterSet.Equals("utf8", StringComparison.OrdinalIgnoreCase)
            || characterSet.Equals("utf8mb3", StringComparison.OrdinalIgnoreCase)
            || characterSet.Equals("utf8mb4", StringComparison.OrdinalIgnoreCase)
            || characterSet.Equals("ucs2", StringComparison.OrdinalIgnoreCase)
            || characterSet.Equals("utf16", StringComparison.OrdinalIgnoreCase)
            || characterSet.Equals("utf32", StringComparison.OrdinalIgnoreCase);
    }

    private static ReferentialAction MapReferentialAction(string? action)
    {
        return action?.ToUpperInvariant() switch
        {
            "CASCADE" => ReferentialAction.Cascade,
            "SET NULL" => ReferentialAction.SetNull,
            "SET DEFAULT" => ReferentialAction.SetDefault,
            "RESTRICT" => ReferentialAction.Restrict,
            _ => ReferentialAction.NoAction,
        };
    }
}
