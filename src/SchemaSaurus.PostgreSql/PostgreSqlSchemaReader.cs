using System.Data;

using Npgsql;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.PostgreSql;

/// <summary>
/// Reads structural metadata from a PostgreSQL database using <c>pg_catalog</c>
/// and <c>information_schema</c>.
/// </summary>
public sealed partial class PostgreSqlSchemaReader : DatabaseSchemaReader<NpgsqlConnection>
{
    private const CommandBehavior SequentialResultBehavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;

    /// <inheritdoc />
    public override string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    protected override async Task ReadDatabaseMetadataAsync(
        NpgsqlConnection connection,
        DatabaseModelBuilder builder,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                current_schema(),
                version(),
                d.datcollate,
                current_setting('server_version_num', true)
            FROM pg_database d
            WHERE d.datname = current_database()
            """;

        const int defaultSchemaNameOrdinal = 0;
        const int serverVersionOrdinal = 1;
        const int collationOrdinal = 2;
        const int compatibilityLevelOrdinal = 3;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;

        var defaultSchemaName = reader.GetStringNull(defaultSchemaNameOrdinal);
        var serverVersion = reader.GetStringNull(serverVersionOrdinal);
        var collation = reader.GetStringNull(collationOrdinal);
        var compatibilityLevel = reader.GetStringNull(compatibilityLevelOrdinal);

        builder
            .WithDefaultSchemaName(defaultSchemaName)
            .WithServerVersion(serverVersion)
            .WithCollation(collation)
            .WithEngineEdition("PostgreSQL")
            .WithCompatibilityLevel(compatibilityLevel);
    }

    private async Task ReadRelationColumnsAsync<TBuilder>(
        NpgsqlConnection connection,
        Dictionary<uint, TBuilder> relationBuilders,
        string relationFilter,
        CancellationToken cancellationToken)
        where TBuilder : class
    {
        var sql = $"""
            SELECT
                cls.oid,
                attr.attnum,
                attr.attname,
                typ.typname,
                basetyp.typname AS basetypname,
                format_type(typ.oid, attr.atttypmod) AS formatted_typname,
                format_type(basetyp.oid, typ.typtypmod) AS formatted_basetypname,
                elemtyp.typname AS elemtypname,
                elem_basetyp.typname AS elem_basetypname,
                base_elemtyp.typname AS base_elemtypname,
                base_elem_basetyp.typname AS base_elem_basetypname,
                NOT (attr.attnotnull OR typ.typnotnull) AS nullable,
                CASE WHEN attr.atthasdef THEN pg_get_expr(def.adbin, cls.oid) END AS default_sql,
                attr.attidentity::text,
                attr.attgenerated::text,
                des.description,
                coll.collname,
                seq.seqstart,
                seq.seqincrement
            FROM pg_class AS cls
            JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
            JOIN pg_attribute AS attr ON attr.attrelid = cls.oid
            JOIN pg_type AS typ ON attr.atttypid = typ.oid
            LEFT JOIN pg_type AS basetyp ON basetyp.oid = typ.typbasetype
            LEFT JOIN pg_type AS elemtyp ON elemtyp.oid = typ.typelem AND typ.typelem <> 0
            LEFT JOIN pg_type AS elem_basetyp ON elem_basetyp.oid = elemtyp.typbasetype
            LEFT JOIN pg_type AS base_elemtyp ON base_elemtyp.oid = basetyp.typelem AND basetyp.typelem <> 0
            LEFT JOIN pg_type AS base_elem_basetyp ON base_elem_basetyp.oid = base_elemtyp.typbasetype
            LEFT JOIN pg_attrdef AS def ON def.adrelid = cls.oid AND def.adnum = attr.attnum
            LEFT JOIN pg_description AS des ON des.objoid = cls.oid AND des.objsubid = attr.attnum
            LEFT JOIN pg_collation AS coll ON coll.oid = attr.attcollation
            LEFT JOIN LATERAL (
                SELECT sequence.seqstart, sequence.seqincrement
                FROM pg_depend AS dep
                JOIN pg_sequence AS sequence ON sequence.seqrelid = dep.objid
                WHERE dep.refobjid = cls.oid
                  AND dep.refobjsubid = attr.attnum
                  AND dep.deptype IN ('i', 'a')
                ORDER BY CASE dep.deptype WHEN 'i' THEN 0 ELSE 1 END
                LIMIT 1
            ) AS seq ON true
            WHERE cls.relkind IN ('r', 'p', 'f', 'v', 'm')
              AND attr.attnum > 0
              AND NOT attr.attisdropped
              AND {relationFilter}
            ORDER BY cls.oid, attr.attnum
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int objectIdOrdinal = 0;
        const int columnIdOrdinal = 1;
        const int columnNameOrdinal = 2;
        const int typeNameOrdinal = 3;
        const int baseTypeNameOrdinal = 4;
        const int formattedTypeOrdinal = 5;
        const int formattedBaseTypeOrdinal = 6;
        const int elementTypeNameOrdinal = 7;
        const int elementBaseTypeNameOrdinal = 8;
        const int baseElementTypeNameOrdinal = 9;
        const int baseElementBaseTypeNameOrdinal = 10;
        const int nullableOrdinal = 11;
        const int defaultOrdinal = 12;
        const int identityOrdinal = 13;
        const int generatedOrdinal = 14;
        const int descriptionOrdinal = 15;
        const int collationOrdinal = 16;
        const int identitySeedOrdinal = 17;
        const int identityIncrementOrdinal = 18;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var objectId = reader.GetFieldValue<uint>(objectIdOrdinal);

            if (!relationBuilders.TryGetValue(objectId, out var relationBuilder))
                continue;

            var columnId = reader.GetInt16(columnIdOrdinal);
            var columnName = reader.GetString(columnNameOrdinal);
            var typeName = reader.GetString(typeNameOrdinal);
            var baseTypeName = reader.GetStringNull(baseTypeNameOrdinal);
            var rawFormattedTypeName = reader.GetString(formattedTypeOrdinal);
            var rawFormattedBaseTypeName = reader.GetStringNull(formattedBaseTypeOrdinal);
            var elementTypeName = reader.GetStringNull(elementTypeNameOrdinal);
            var elementBaseTypeName = reader.GetStringNull(elementBaseTypeNameOrdinal);
            var baseElementTypeName = reader.GetStringNull(baseElementTypeNameOrdinal);
            var baseElementBaseTypeName = reader.GetStringNull(baseElementBaseTypeNameOrdinal);
            var isNullable = reader.GetBoolean(nullableOrdinal);
            var defaultSql = reader.GetStringNull(defaultOrdinal);
            var identityKind = reader.GetString(identityOrdinal);
            var generatedKind = reader.GetString(generatedOrdinal);
            var description = reader.GetStringNull(descriptionOrdinal);
            var collation = reader.GetStringNull(collationOrdinal);
            var identitySeed = reader.GetInt64Null(identitySeedOrdinal);
            var identityIncrement = reader.GetInt64Null(identityIncrementOrdinal);

            var hasBaseType = baseTypeName is not null;
            var formattedTypeName = AdjustFormattedTypeName(rawFormattedTypeName);

            var formattedBaseTypeName = rawFormattedBaseTypeName is not null
                ? AdjustFormattedTypeName(rawFormattedBaseTypeName)
                : null;

            var isIdentity = !string.IsNullOrWhiteSpace(identityKind);
            var isComputed = string.Equals(generatedKind, "s", StringComparison.Ordinal);

            var isArray = elementTypeName is not null || baseElementTypeName is not null;
            var arrayElementTypeName = baseElementBaseTypeName
                ?? baseElementTypeName
                ?? elementBaseTypeName
                ?? elementTypeName;

            var systemTypeName = hasBaseType ? baseTypeName! : typeName;
            var nativeTypeName = formattedBaseTypeName ?? formattedTypeName;

            var (dbType, npgsqlDbType, systemType, isUnicode, isFixedLength) = isArray && arrayElementTypeName is not null
                ? PostgreSqlTypeMapper.MapArrayNativeType(arrayElementTypeName)
                : PostgreSqlTypeMapper.MapNativeType(systemTypeName);

            int? maxLength = isArray ? null : GetMaxLength(nativeTypeName);

            int? precision = isArray ? null : GetPrecision(nativeTypeName);

            int? scale = isArray ? null : GetScale(nativeTypeName);

            var computedSql = isComputed ? defaultSql : null;
            var columnDefaultSql = isComputed ? null : defaultSql;

            void Configure(ColumnBuilder columnBuilder)
            {
                columnBuilder
                    .WithName(columnName)
                    .WithOrdinalPosition(columnId)
                    .WithIsNullable(isNullable)
                    .WithDefaultValueSql(columnDefaultSql)
                    .WithIsIdentity(isIdentity)
                    .WithIdentitySeed(identitySeed)
                    .WithIdentityIncrement(identityIncrement)
                    .WithIsComputed(isComputed)
                    .WithComputedColumnSql(computedSql)
                    .WithIsStored(isComputed)
                    .WithCollation(collation == "default" ? null : collation)
                    .WithDescription(description)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLength)
                    .WithPrecision(precision)
                    .WithScale(scale)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength)
                    .WithAnnotation(PostgreSqlAnnotations.NpgsqlDbType, npgsqlDbType.ToString());
            }

            if (relationBuilder is TableBuilder tableBuilder)
                tableBuilder.AddColumn(Configure);
            else if (relationBuilder is ViewBuilder viewBuilder)
                viewBuilder.AddColumn(Configure);
        }
    }


    private static string BuildTableFilter(SchemaReaderOptions options, string schemaExpression, string tableExpression)
    {
        List<string> conditions = [$"{schemaExpression} NOT IN ('pg_catalog', 'information_schema')"];

        var schemaFilter = BuildSchemaFilter(options.Schemas, schemaExpression);
        if (schemaFilter is not null)
            conditions.Add(schemaFilter);

        if (options.Tables.Count > 0)
        {
            var list = string.Join(", ", options.Tables.Select(value => value.EscapeLiteral()));
            conditions.Add($"{tableExpression} IN ({list})");
        }

        return string.Join("\n              AND ", conditions);
    }

    private static string? BuildSchemaFilter(IReadOnlyCollection<string> schemas, string schemaExpression)
    {
        if (schemas.Count == 0)
            return null;

        var list = string.Join(", ", schemas.Select(value => value.EscapeLiteral()));
        return $"{schemaExpression} IN ({list})";
    }

    private static ColumnReference[] CreateColumnReferences(string[] columnNames)
    {
        var references = new ColumnReference[columnNames.Length];
        for (var i = 0; i < columnNames.Length; i++)
        {
            references[i] = new()
            {
                ColumnName = columnNames[i],
            };
        }

        return references;
    }

    private static void AddStorageParameterAnnotations<TBuilder>(TBuilder builder, string[]? storageParameters)
        where TBuilder : IAnnotationBuilder<TBuilder>
    {
        if (storageParameters is null)
            return;

        foreach (var storageParameter in storageParameters)
        {
            var separatorIndex = storageParameter.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
                continue;

            var annotationName = storageParameter.AsSpan(0, separatorIndex);
            if (annotationName.IsEmpty)
                continue;

            builder.WithAnnotation(annotationName.ToString(), storageParameter[(separatorIndex + 1)..]);
        }
    }

    private static string AdjustFormattedTypeName(string formattedTypeName)
    {
        if (formattedTypeName.Length > 1 && formattedTypeName[0] == '"')
            formattedTypeName = formattedTypeName[1..^1];

        return formattedTypeName == "bpchar" ? "char" : formattedTypeName;
    }

    private static string NormalizeSequenceTypeName(string typeName)
    {
        return typeName switch
        {
            "int2" => "smallint",
            "int4" => "integer",
            "int8" => "bigint",
            _ => typeName,
        };
    }


    private static int? GetMaxLength(string nativeTypeName)
    {
        var span = nativeTypeName.AsSpan();

        var open = span.IndexOf('(');
        var close = span.IndexOf(')');

        if (open < 0 || close <= open)
            return null;

        var value = span[(open + 1)..close];
        if (value.Contains(','))
            return null;

        return int.TryParse(value, out var length) ? length : null;
    }

    private static int? GetPrecision(string nativeTypeName)
    {
        var values = GetNumericFacets(nativeTypeName);
        return values.Precision;
    }

    private static int? GetScale(string nativeTypeName)
    {
        var values = GetNumericFacets(nativeTypeName);
        return values.Scale;
    }

    private static (int? Precision, int? Scale) GetNumericFacets(string nativeTypeName)
    {
        var span = nativeTypeName.AsSpan();

        var open = span.IndexOf('(');
        var close = span.IndexOf(')');

        if (open < 0 || close <= open)
            return (null, null);

        var value = span[(open + 1)..close];
        var separatorIndex = value.IndexOf(',');

        if (separatorIndex < 0)
            return (null, null);

        var precisionValue = value[..separatorIndex].Trim();
        var scaleValue = value[(separatorIndex + 1)..].Trim();

        var hasPrecision = int.TryParse(precisionValue, out var precision);
        var hasScale = int.TryParse(scaleValue, out var scale);

        return (hasPrecision ? precision : null, hasScale ? scale : null);
    }


    private static ReferentialAction MapReferentialAction(string? action)
    {
        return action switch
        {
            "r" => ReferentialAction.Restrict,
            "c" => ReferentialAction.Cascade,
            "n" => ReferentialAction.SetNull,
            "d" => ReferentialAction.SetDefault,
            _ => ReferentialAction.NoAction,
        };
    }

    private static UserDefinedTypeKind MapUserDefinedTypeKind(string kind)
    {
        return kind switch
        {
            "c" => UserDefinedTypeKind.Composite,
            "e" => UserDefinedTypeKind.Enum,
            "d" => UserDefinedTypeKind.Domain,
            _ => UserDefinedTypeKind.Alias,
        };
    }

}
