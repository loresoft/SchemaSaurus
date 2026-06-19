using System.Data;

using NpgsqlTypes;

namespace SchemaSaurus.PostgreSql.Tests;

public class PostgreSqlDataTypeMapperTests
{
    [Theory]
    [InlineData(DbType.Int64, NpgsqlDbType.Bigint)]
    [InlineData(DbType.String, NpgsqlDbType.Text)]
    [InlineData(DbType.Binary, NpgsqlDbType.Bytea)]
    [InlineData(DbType.Guid, NpgsqlDbType.Uuid)]
    [InlineData(DbType.UInt32, NpgsqlDbType.Xid)]
    [InlineData(DbType.UInt64, NpgsqlDbType.Xid8)]
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
    [InlineData(NpgsqlDbType.Oid, DbType.UInt32)]
    [InlineData(NpgsqlDbType.Xid, DbType.UInt32)]
    [InlineData(NpgsqlDbType.Xid8, DbType.UInt64)]
    [InlineData(NpgsqlDbType.Xml, DbType.Xml)]
    [InlineData(NpgsqlDbType.Json, DbType.String)]
    [InlineData(NpgsqlDbType.Jsonb, DbType.String)]
    [InlineData(NpgsqlDbType.JsonPath, DbType.String)]
    [InlineData(NpgsqlDbType.Geometry, DbType.Object)]
    [InlineData(NpgsqlDbType.Geography, DbType.Object)]
    [InlineData(NpgsqlDbType.Citext, DbType.Object)]
    public void WhenMappingNpgsqlDbTypeThenDbTypeIsReturned(NpgsqlDbType npgsqlDbType, DbType expectedDbType)
    {
        var dbType = PostgreSqlTypeMapper.ToDbType(npgsqlDbType);

        dbType.Should().Be(expectedDbType);
    }

    [Theory]
    [InlineData("json", DbType.String, NpgsqlDbType.Json, typeof(string), true, false)]
    [InlineData("jsonb", DbType.String, NpgsqlDbType.Jsonb, typeof(string), true, false)]
    [InlineData("uuid", DbType.Guid, NpgsqlDbType.Uuid, typeof(Guid), null, null)]
    [InlineData("oid", DbType.UInt32, NpgsqlDbType.Oid, typeof(uint), null, null)]
    [InlineData("xid", DbType.UInt32, NpgsqlDbType.Xid, typeof(uint), null, null)]
    [InlineData("xid8", DbType.UInt64, NpgsqlDbType.Xid8, typeof(ulong), null, null)]
    [InlineData("citext", DbType.String, NpgsqlDbType.Citext, typeof(string), true, false)]
    [InlineData("jsonpath", DbType.String, NpgsqlDbType.JsonPath, typeof(string), true, false)]
    [InlineData("bit", DbType.StringFixedLength, NpgsqlDbType.Bit, typeof(string), null, true)]
    [InlineData("varbit", DbType.String, NpgsqlDbType.Varbit, typeof(string), null, false)]
    [InlineData("inet", DbType.Object, NpgsqlDbType.Inet, typeof(NpgsqlInet), null, null)]
    [InlineData("macaddr", DbType.Object, NpgsqlDbType.MacAddr, typeof(System.Net.NetworkInformation.PhysicalAddress), null, null)]
    [InlineData("pg_lsn", DbType.Object, NpgsqlDbType.PgLsn, typeof(NpgsqlLogSequenceNumber), null, null)]
    [InlineData("tid", DbType.Object, NpgsqlDbType.Tid, typeof(NpgsqlTid), null, null)]
    [InlineData("geometry", DbType.Object, NpgsqlDbType.Geometry, typeof(object), null, null)]
    [InlineData("geography", DbType.Object, NpgsqlDbType.Geography, typeof(object), null, null)]
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
    public void WhenMappingIntegerArrayNativeTypeThenExpectedMetadataIsReturned()
    {
        var expectedNpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer;

        var mapping = PostgreSqlTypeMapper.MapArrayNativeType("int4");

        mapping.DbType.Should().Be(DbType.Object);
        mapping.NpgsqlDbType.Should().Be(expectedNpgsqlDbType);
        mapping.SystemType.Should().Be(typeof(int[]));
        mapping.IsUnicode.Should().BeNull();
        mapping.IsFixedLength.Should().BeNull();
    }

    [Fact]
    public void WhenMappingTextArrayNativeTypeThenExpectedMetadataIsReturned()
    {
        var expectedNpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text;

        var mapping = PostgreSqlTypeMapper.MapNativeType("text[]");

        mapping.DbType.Should().Be(DbType.Object);
        mapping.NpgsqlDbType.Should().Be(expectedNpgsqlDbType);
        mapping.SystemType.Should().Be(typeof(string[]));
        mapping.IsUnicode.Should().BeTrue();
        mapping.IsFixedLength.Should().BeFalse();
    }

    [Fact]
    public void WhenMappingUnknownArrayNativeTypeThenObjectArrayMetadataIsReturned()
    {
        var expectedNpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Unknown;

        var mapping = PostgreSqlTypeMapper.MapArrayNativeType("custom_type");

        mapping.DbType.Should().Be(DbType.Object);
        mapping.NpgsqlDbType.Should().Be(expectedNpgsqlDbType);
        mapping.SystemType.Should().Be(typeof(object[]));
        mapping.IsUnicode.Should().BeNull();
        mapping.IsFixedLength.Should().BeNull();
    }

    [Fact]
    public void WhenMappingUnsupportedDbTypeThenUnknownIsReturned()
    {
        var npgsqlDbType = PostgreSqlTypeMapper.ToNpgsqlDbType(DbType.SByte);

        npgsqlDbType.Should().Be(NpgsqlDbType.Unknown);
    }
}
