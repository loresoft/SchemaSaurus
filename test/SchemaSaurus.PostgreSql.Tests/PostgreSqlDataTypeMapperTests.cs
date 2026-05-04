using System.Data;

using NpgsqlTypes;

using SchemaSaurus.PostgreSQL;

namespace SchemaSaurus.PostgreSql.Tests;

public class PostgreSqlDataTypeMapperTests
{
    [Theory]
    [InlineData(DbType.Int64, NpgsqlDbType.Bigint)]
    [InlineData(DbType.String, NpgsqlDbType.Text)]
    [InlineData(DbType.Binary, NpgsqlDbType.Bytea)]
    [InlineData(DbType.Guid, NpgsqlDbType.Uuid)]
    [InlineData(DbType.Xml, NpgsqlDbType.Xml)]
    public void WhenMappingDbTypeThenNpgsqlDbTypeIsReturned(DbType dbType, NpgsqlDbType expectedNpgsqlDbType)
    {
        var npgsqlDbType = PostgreSqlTypeMapper.ToNpgsqlDbType(dbType);

        npgsqlDbType.Should().Be(expectedNpgsqlDbType);
    }

    [Theory]
    [InlineData(NpgsqlDbType.Bigint, DbType.Int64)]
    [InlineData(NpgsqlDbType.Text, DbType.String)]
    [InlineData(NpgsqlDbType.Varchar, DbType.String)]
    [InlineData(NpgsqlDbType.Bytea, DbType.Binary)]
    [InlineData(NpgsqlDbType.Uuid, DbType.Guid)]
    [InlineData(NpgsqlDbType.Xml, DbType.Xml)]
    [InlineData(NpgsqlDbType.Json, DbType.String)]
    [InlineData(NpgsqlDbType.Jsonb, DbType.String)]
    public void WhenMappingNpgsqlDbTypeThenDbTypeIsReturned(NpgsqlDbType npgsqlDbType, DbType expectedDbType)
    {
        var dbType = PostgreSqlTypeMapper.ToDbType(npgsqlDbType);

        dbType.Should().Be(expectedDbType);
    }

    [Theory]
    [InlineData("json", DbType.String, NpgsqlDbType.Json, typeof(string), true, false)]
    [InlineData("jsonb", DbType.String, NpgsqlDbType.Jsonb, typeof(string), true, false)]
    [InlineData("uuid", DbType.Guid, NpgsqlDbType.Uuid, typeof(Guid), null, null)]
    public void WhenMappingNativeTypeThenExpectedMetadataIsReturned(
        string typeName,
        DbType expectedDbType,
        NpgsqlDbType expectedNpgsqlDbType,
        Type expectedSystemType,
        bool? expectedIsUnicode,
        bool? expectedIsFixedLength)
    {
        var mapping = PostgreSqlTypeMapper.MapNativeType(typeName);

        mapping.DbType.Should().Be(expectedDbType);
        mapping.NpgsqlDbType.Should().Be(expectedNpgsqlDbType);
        mapping.SystemType.Should().Be(expectedSystemType);
        mapping.IsUnicode.Should().Be(expectedIsUnicode);
        mapping.IsFixedLength.Should().Be(expectedIsFixedLength);
    }

    [Fact]
    public void WhenMappingUnsupportedDbTypeThenUnknownIsReturned()
    {
        var npgsqlDbType = PostgreSqlTypeMapper.ToNpgsqlDbType(DbType.SByte);

        npgsqlDbType.Should().Be(NpgsqlDbType.Unknown);
    }
}
