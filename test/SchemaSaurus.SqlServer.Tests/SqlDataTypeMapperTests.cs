using System.Data;

namespace SchemaSaurus.SqlServer.Tests;

public class SqlDataTypeMapperTests
{
    [Theory]
    [InlineData(DbType.Int64, SqlDbType.BigInt)]
    [InlineData(DbType.AnsiString, SqlDbType.VarChar)]
    [InlineData(DbType.String, SqlDbType.NVarChar)]
    [InlineData(DbType.Binary, SqlDbType.VarBinary)]
    [InlineData(DbType.Guid, SqlDbType.UniqueIdentifier)]
    [InlineData(DbType.Xml, SqlDbType.Xml)]
    public void WhenMappingDbTypeThenSqlDbTypeIsReturned(DbType dbType, SqlDbType expectedSqlDbType)
    {
        var sqlDbType = SqlServerTypeMapper.ToSqlDbType(dbType);

        sqlDbType.Should().Be(expectedSqlDbType);
    }

    [Theory]
    [InlineData(SqlDbType.BigInt, DbType.Int64)]
    [InlineData(SqlDbType.VarChar, DbType.AnsiString)]
    [InlineData(SqlDbType.NVarChar, DbType.String)]
    [InlineData(SqlDbType.VarBinary, DbType.Binary)]
    [InlineData(SqlDbType.UniqueIdentifier, DbType.Guid)]
    [InlineData(SqlDbType.Xml, DbType.Xml)]
    [InlineData(SqlDbType.Json, DbType.String)]
    [InlineData(SqlDbType.Vector, DbType.Object)]
    public void WhenMappingSqlDbTypeThenDbTypeIsReturned(SqlDbType sqlDbType, DbType expectedDbType)
    {
        var dbType = SqlServerTypeMapper.ToDbType(sqlDbType);

        dbType.Should().Be(expectedDbType);
    }

    [Theory]
    [InlineData("json", DbType.String, SqlDbType.Json, typeof(string), true, false)]
    [InlineData("vector", DbType.Object, SqlDbType.Vector, typeof(float[]), null, null)]
    public void WhenMappingNativeTypeThenExpectedMetadataIsReturned(
        string typeName,
        DbType expectedDbType,
        SqlDbType expectedSqlDbType,
        Type expectedSystemType,
        bool? expectedIsUnicode,
        bool? expectedIsFixedLength)
    {
        var mapping = SqlServerTypeMapper.MapNativeType(typeName);

        mapping.DbType.Should().Be(expectedDbType);
        mapping.SqlDbType.Should().Be(expectedSqlDbType);
        mapping.SystemType.Should().Be(expectedSystemType);
        mapping.IsUnicode.Should().Be(expectedIsUnicode);
        mapping.IsFixedLength.Should().Be(expectedIsFixedLength);
    }

    [Fact]
    public void WhenMappingUnsupportedDbTypeThenVariantIsReturned()
    {
        var sqlDbType = SqlServerTypeMapper.ToSqlDbType(DbType.SByte);

        sqlDbType.Should().Be(SqlDbType.Variant);
    }
}
