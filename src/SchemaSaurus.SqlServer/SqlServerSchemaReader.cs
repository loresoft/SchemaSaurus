using System.Data;
using System.Globalization;

using Microsoft.Data.SqlClient;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.SqlServer;

/// <summary>
/// Reads structural metadata from a SQL Server database using <c>sys.*</c> catalog views.
/// Schema/table filtering is pushed into SQL WHERE clauses. Extended properties (MS_Description)
/// are joined inline. Large read methods are decomposed into focused private sub-methods.
/// </summary>
public sealed partial class SqlServerSchemaReader : DatabaseSchemaReader<SqlConnection>
{
    private const CommandBehavior SequentialResultBehavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;

    private readonly Dictionary<(int Class, int MajorId, int MinorId), List<KeyValuePair<string, object?>>> _extendedProperties = [];

    /// <inheritdoc />
    public override string ProviderName => "SqlServer";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        SqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        // Read extended properties first so that they can be applied to the relevant metadata elements as we read them,
        // without needing to do lookups back into the database later.
        await ReadExtendedPropertiesAsync(connection, cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT
                CAST(SERVERPROPERTY('Collation') AS NVARCHAR(256))  AS collation,
                SCHEMA_NAME()                                       AS default_schema,
                @@VERSION                                           AS server_version,
                CAST(SERVERPROPERTY('Edition') AS NVARCHAR(256))    AS edition,
                CAST(SERVERPROPERTY('EngineEdition') AS INT)        AS engine_edition,
                (SELECT compatibility_level
                 FROM sys.databases
                 WHERE name = DB_NAME())                            AS compat_level
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        const int collationOrdinal = 0;
        const int schemaOrdinal = 1;
        const int versionOrdinal = 2;
        const int editionOrdinal = 3;
        const int engineOrdinal = 4;
        const int compatOrdinal = 5;

        var collation = reader.GetStringNull(collationOrdinal);
        var defaultSchema = reader.GetStringNull(schemaOrdinal);
        var serverVersion = reader.GetStringNull(versionOrdinal);
        var edition = reader.GetStringNull(editionOrdinal);
        var engineEditionValue = reader.GetInt32Null(engineOrdinal);
        var compatibilityLevelValue = reader.GetByteNull(compatOrdinal);

        var engineEdition = engineEditionValue is null ? null : GetEngineEditionName(engineEditionValue.Value);
        var compatibilityLevel = compatibilityLevelValue?.ToString(CultureInfo.InvariantCulture);

        builder
            .WithCollation(collation)
            .WithDefaultSchemaName(defaultSchema)
            .WithServerVersion(serverVersion)
            .WithEdition(edition)
            .WithEngineEdition(engineEdition)
            .WithCompatibilityLevel(compatibilityLevel);

        // Apply extended properties to the database itself (class=0, major_id=0, minor_id=0).
        ApplyExtendedProperties((0, 0, 0), builder);
    }


    private async Task ReadExtendedPropertiesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        _extendedProperties.Clear();

        const string sql = """
            SELECT
                ep.class,
                ep.major_id,
                ep.minor_id,
                ep.name,
                ep.value
            FROM sys.extended_properties ep
            ORDER BY ep.class, ep.major_id, ep.minor_id, ep.name
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int classOrdinal = 0;
        const int majorIdOrdinal = 1;
        const int minorIdOrdinal = 2;
        const int nameOrdinal = 3;
        const int valueOrdinal = 4;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var classId = reader.GetByte(classOrdinal);
            var majorId = reader.GetInt32(majorIdOrdinal);
            var minorId = reader.GetInt32(minorIdOrdinal);

            var name = reader.GetString(nameOrdinal);
            var value = reader.GetValueNull(valueOrdinal);

            var key = (classId, majorId, minorId);
            if (!_extendedProperties.TryGetValue(key, out var values))
            {
                values = [];
                _extendedProperties[key] = values;
            }

            values.Add(new KeyValuePair<string, object?>(name, value));
        }
    }

    private async Task ReadParametersAsync<TBuilder>(
        SqlConnection connection,
        Dictionary<int, TBuilder> builders,
        string objectTypeFilter,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        var sql = $"""
            SELECT
                par.object_id,
                par.name                        AS param_name,
                par.parameter_id,
                st.name                         AS system_type_name,
                TYPE_NAME(par.user_type_id)     AS user_type_name,
                par.max_length,
                par.precision,
                par.scale,
                par.is_output
            FROM sys.parameters par
            INNER JOIN sys.objects o ON par.object_id = o.object_id
            INNER JOIN sys.types st
                ON par.system_type_id = st.system_type_id
                AND st.system_type_id = st.user_type_id
            WHERE o.is_ms_shipped = 0 AND par.parameter_id > 0 AND {objectTypeFilter}
            ORDER BY par.object_id, par.parameter_id
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int nameOrdinal = 1;
        const int paramIdOrdinal = 2;
        const int sysTypeOrdinal = 3;
        const int userTypeOrdinal = 4;
        const int maxLenOrdinal = 5;
        const int precisionOrdinal = 6;
        const int scaleOrdinal = 7;
        const int outputOrdinal = 8;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetInt32(objectIdOrdinal);

            if (!builders.TryGetValue(objectId, out var b))
                continue;

            var paramName = reader.GetString(nameOrdinal);
            var paramOrdinal = reader.GetInt32(paramIdOrdinal);
            var systemTypeName = reader.GetString(sysTypeOrdinal);
            var userTypeName = reader.GetStringNull(userTypeOrdinal) ?? systemTypeName;
            var maxLength = reader.GetInt16(maxLenOrdinal);
            var precision = reader.GetByte(precisionOrdinal);
            var scale = reader.GetByte(scaleOrdinal);
            var isOutput = reader.GetBoolean(outputOrdinal);

            var maxLengthValue = NormalizeMaxLength(systemTypeName, maxLength);
            byte? precisionValue = HasPrecision(systemTypeName) ? precision : null;
            var scaleValue = HasScale(systemTypeName) ? (int?)scale : null;

            var (dbType, sqlDbType, systemType, isUnicode, isFixedLength) = SqlServerTypeMapper.MapNativeType(systemTypeName);
            var nativeTypeName = FormatNativeTypeName(systemTypeName, userTypeName, maxLength, precision, scale);

            var direction = isOutput ? Metadata.ParameterDirection.Output : Metadata.ParameterDirection.Input;

            void Configure(ParameterBuilder parameterBuilder)
            {
                parameterBuilder
                    .WithName(paramName)
                    .WithOrdinal(paramOrdinal)
                    .WithDirection(direction)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLengthValue)
                    .WithPrecision(precisionValue)
                    .WithScale(scaleValue)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength);

                ApplyExtendedProperties((2, objectId, paramOrdinal), parameterBuilder);
                parameterBuilder.WithAnnotation(SqlServerAnnotations.SqlDbType, sqlDbType.ToString());
            }

            if (b is StoredProcedureBuilder spb)
                spb.AddParameter(Configure);
            else if (b is TableValuedFunctionBuilder tvfb)
                tvfb.AddParameter(Configure);
        }
    }


    private void ApplyExtendedProperties<TBuilder>(
        (int Class, int MajorId, int MinorId) key,
        TBuilder targetBuilder)
        where TBuilder : IAnnotationBuilder<TBuilder>
    {
        if (!_extendedProperties.TryGetValue(key, out var values))
            return;

        foreach (var (name, value) in values)
            targetBuilder.WithAnnotation(name, value);
    }


    private static string? GetEngineEditionName(int engineEdition)
    {
        return engineEdition switch
        {
            1 => "Personal",
            2 => "Standard",
            3 => "Enterprise",
            4 => "Express",
            5 => "AzureSQLDatabase",
            6 => "AzureSynapseAnalytics",
            8 => "AzureSQLManagedInstance",
            9 => "AzureSQLEdge",
            11 => "AzureSynapseServerless",
            _ => "Unknown"
        };
    }


    private static string BuildTableFilter(SchemaReaderOptions options)
    {
        // Always filter out system objects (is_ms_shipped = 0) and then apply additional filters based on the specified schemas and tables if provided.
        var conditions = new List<string> { "t.is_ms_shipped = 0" };

        // If specific schemas are specified in the options, add a filter condition to include only those schemas.
        if (options.Schemas.Count > 0)
        {
            var list = string.Join(", ", options.Schemas.Select(EscapeUnicodeLiteral));
            conditions.Add($"SCHEMA_NAME(t.schema_id) IN ({list})");
        }

        // If specific tables are specified in the options, add a filter condition to include only those tables.
        if (options.Tables.Count > 0)
        {
            var list = string.Join(", ", options.Tables.Select(EscapeUnicodeLiteral));
            conditions.Add($"t.name IN ({list})");
        }

        // Combine all conditions into a single WHERE clause string, joining them with "AND".
        return string.Join("\n    AND ", conditions);
    }

    private static string? BuildSchemaFilter(IReadOnlyCollection<string> schemas, string schemaExpression)
    {
        if (schemas.Count == 0)
            return null;

        // Build a filter condition to include only the specified schemas.
        // The schemaExpression parameter allows specifying the expression to use
        // for the schema name (e.g. "SCHEMA_NAME(o.schema_id)" or "SCHEMA_NAME(t.schema_id)").

        var list = string.Join(", ", schemas.Select(EscapeUnicodeLiteral));

        // Return a filter condition like "SCHEMA_NAME(o.schema_id) IN ('schema1', 'schema2')".
        return $"{schemaExpression} IN ({list})";
    }


    private static string EscapeUnicodeLiteral(string value)
        => $"N{value.EscapeLiteral()}";


    private static string FormatNativeTypeName(string systemTypeName, string userTypeName, short maxLength, byte precision, byte scale)
    {
        // User-defined alias type — return the alias name as-is.
        if (!string.Equals(systemTypeName, userTypeName, StringComparison.OrdinalIgnoreCase))
            return userTypeName;

        // For system types, format the native type name with length/precision/scale as appropriate for the type.
        // For example, for character types, include the max length (e.g. varchar(50)); for decimal/numeric, include precision and scale (e.g. decimal(18, 2));
        // for datetime2/time/datetimeoffset, include fractional seconds precision if it's not the default of 7 (e.g. datetime2(3)).

        var nativeTypeName = systemTypeName.ToUpperInvariant();

        if (IsTypeName(systemTypeName, "char") || IsTypeName(systemTypeName, "varchar") || IsTypeName(systemTypeName, "binary") || IsTypeName(systemTypeName, "varbinary"))
            return maxLength == -1 ? $"{nativeTypeName}(MAX)" : $"{nativeTypeName}({maxLength})";

        if (IsTypeName(systemTypeName, "nchar") || IsTypeName(systemTypeName, "nvarchar"))
            return maxLength == -1 ? $"{nativeTypeName}(MAX)" : $"{nativeTypeName}({maxLength / 2})";

        if (IsTypeName(systemTypeName, "decimal") || IsTypeName(systemTypeName, "numeric"))
            return $"{nativeTypeName}({precision}, {scale})";

        if (IsTypeName(systemTypeName, "datetime2") || IsTypeName(systemTypeName, "datetimeoffset") || IsTypeName(systemTypeName, "time"))
            return scale != 7 ? $"{nativeTypeName}({scale})" : nativeTypeName;

        return nativeTypeName;
    }

    private static int? NormalizeMaxLength(string systemTypeName, short maxLength)
    {
        if (IsTypeName(systemTypeName, "char") || IsTypeName(systemTypeName, "varchar") || IsTypeName(systemTypeName, "binary") || IsTypeName(systemTypeName, "varbinary"))
            return maxLength == -1 ? null : maxLength;

        if (IsTypeName(systemTypeName, "nchar") || IsTypeName(systemTypeName, "nvarchar"))
            return maxLength == -1 ? null : maxLength / 2;

        return null;
    }

    private static bool HasPrecision(string systemTypeName)
        => IsTypeName(systemTypeName, "decimal")
        || IsTypeName(systemTypeName, "numeric");

    private static bool HasScale(string systemTypeName)
        => IsTypeName(systemTypeName, "decimal")
        || IsTypeName(systemTypeName, "numeric")
        || IsTypeName(systemTypeName, "datetime2")
        || IsTypeName(systemTypeName, "datetimeoffset")
        || IsTypeName(systemTypeName, "time");

    private static bool IsTypeName(string typeName, string expected)
        => string.Equals(typeName, expected, StringComparison.OrdinalIgnoreCase);

    private static ReferentialAction MapReferentialAction(byte action) => action switch
    {
        1 => ReferentialAction.Cascade,
        2 => ReferentialAction.SetNull,
        3 => ReferentialAction.SetDefault,
        _ => ReferentialAction.NoAction,
    };
}
