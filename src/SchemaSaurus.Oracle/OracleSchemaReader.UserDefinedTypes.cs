using System.Data;

using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;
using SchemaSaurus.Metadata.Provider;

namespace SchemaSaurus.Oracle;

public sealed partial class OracleSchemaReader
{
    /// <inheritdoc />
    protected override async Task ReadUserDefinedTypesAsync(
        OracleConnection connection,
        DatabaseModelBuilder builder,
        SchemaReaderOptions options,
        CancellationToken cancellationToken)
    {
        var schemaFilter = BuildSchemaFilter(options.Schemas, "t.OWNER");

        var sql = $"""
            SELECT
                t.OWNER,
                t.TYPE_NAME,
                t.TYPECODE
            FROM ALL_TYPES t
            WHERE {schemaFilter}
            ORDER BY t.OWNER, t.TYPE_NAME
            """;

        var types = new Dictionary<(string Schema, string Name), UserDefinedTypeBuilder>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

            const int schemaOrdinal = 0;
            const int nameOrdinal = 1;
            const int typeCodeOrdinal = 2;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var schema = reader.GetString(schemaOrdinal);
                var name = reader.GetString(nameOrdinal);
                var typeCode = reader.GetString(typeCodeOrdinal);

                var kind = typeCode.Equals("OBJECT", StringComparison.OrdinalIgnoreCase)
                    ? UserDefinedTypeKind.Composite
                    : UserDefinedTypeKind.Alias;

                var typeBuilder = new UserDefinedTypeBuilder()
                    .WithSchemaQualifiedName(schema, name)
                    .WithKind(kind)
                    .WithDbType(DbType.Object)
                    .WithNativeTypeName(name)
                    .WithSystemType(typeof(object))
                    .WithAnnotation(OracleAnnotations.OracleDbType, OracleDbType.Object.ToString())
                    .WithAnnotation(OracleAnnotations.TypeCode, typeCode);

                types[(schema, name)] = typeBuilder;
            }
        }

        await ReadUserDefinedTypeAttributesAsync(connection, types, cancellationToken).ConfigureAwait(false);

        foreach (var (_, typeBuilder) in types)
        {
            var userDefinedType = typeBuilder.Build();
            builder.AddUserDefinedType(userDefinedType);
        }
    }

    private async Task ReadUserDefinedTypeAttributesAsync(
        OracleConnection connection,
        Dictionary<(string Schema, string Name), UserDefinedTypeBuilder> types,
        CancellationToken cancellationToken)
    {
        if (types.Count == 0)
            return;

        const string sql = """
            SELECT
                a.OWNER,
                a.TYPE_NAME,
                a.ATTR_NAME,
                a.ATTR_NO,
                a.ATTR_TYPE_NAME,
                a.LENGTH,
                a.PRECISION,
                a.SCALE
            FROM ALL_TYPE_ATTRS a
            ORDER BY a.OWNER, a.TYPE_NAME, a.ATTR_NO
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int schemaOrdinal = 0;
        const int typeOrdinal = 1;
        const int nameOrdinal = 2;
        const int attributeOrdinal = 3;
        const int dataTypeOrdinal = 4;
        const int lengthOrdinal = 5;
        const int precisionOrdinal = 6;
        const int scaleOrdinal = 7;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.GetString(schemaOrdinal);
            var typeName = reader.GetString(typeOrdinal);

            if (!types.TryGetValue((schema, typeName), out var typeBuilder))
                continue;

            var attributeName = reader.GetString(nameOrdinal);
            var attributeOrdinalPosition = GetInt32Null(reader.GetValue(attributeOrdinal)) ?? 0;
            var dataType = reader.GetString(dataTypeOrdinal);
            var dataLength = GetInt32Null(reader.GetValueNull(lengthOrdinal));
            var precision = GetInt32Null(reader.GetValueNull(precisionOrdinal));
            var scale = GetInt32Null(reader.GetValueNull(scaleOrdinal));


            var nativeTypeName = FormatNativeTypeName(dataType, dataLength, dataLength, precision, scale);
            var maxLength = GetMaxLength(dataType, dataLength, dataLength);

            var (dbType, oracleDbType, systemType, isUnicode, isFixedLength) = MapOracleType(dataType, dataLength, precision, scale);

            var precisionValue = NormalizePrecision(dbType, precision);
            var scaleValue = NormalizeScale(dbType, scale);

            typeBuilder.AddColumn(columnBuilder =>
            {
                columnBuilder
                    .WithName(attributeName)
                    .WithOrdinalPosition(attributeOrdinalPosition)
                    .WithIsNullable(true)
                    .WithNativeTypeName(nativeTypeName)
                    .WithDbType(dbType)
                    .WithSystemType(systemType)
                    .WithMaxLength(maxLength)
                    .WithPrecision(precisionValue)
                    .WithScale(scaleValue)
                    .WithIsUnicode(isUnicode)
                    .WithIsFixedLength(isFixedLength)
                    .WithAnnotation(OracleAnnotations.OracleDbType, oracleDbType.ToString());
            });
        }
    }
}
