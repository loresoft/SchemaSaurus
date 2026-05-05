using System.Data;

using MySqlConnector;

namespace SchemaSaurus.MySql.Tests;

public class DataTypeMapperTests
{
    [Theory]
    [InlineData(DbType.Int64, MySqlDbType.Int64)]
    [InlineData(DbType.AnsiString, MySqlDbType.VarChar)]
    [InlineData(DbType.String, MySqlDbType.VarChar)]
    [InlineData(DbType.Binary, MySqlDbType.Blob)]
    [InlineData(DbType.Guid, MySqlDbType.Guid)]
    [InlineData(DbType.Object, MySqlDbType.JSON)]
    public void WhenMappingDbTypeThenMySqlDbTypeIsReturned(DbType dbType, MySqlDbType expectedMySqlDbType)
    {
        var mySqlDbType = MySqlTypeMapper.ToMySqlDbType(dbType);

        mySqlDbType.Should().Be(expectedMySqlDbType);
    }

    [Theory]
    [InlineData(MySqlDbType.Int64, DbType.Int64)]
    [InlineData(MySqlDbType.VarChar, DbType.String)]
    [InlineData(MySqlDbType.Blob, DbType.Binary)]
    [InlineData(MySqlDbType.Guid, DbType.Guid)]
    [InlineData(MySqlDbType.JSON, DbType.String)]
    [InlineData(MySqlDbType.Geometry, DbType.Object)]
    public void WhenMappingMySqlDbTypeThenDbTypeIsReturned(MySqlDbType mySqlDbType, DbType expectedDbType)
    {
        var dbType = MySqlTypeMapper.ToDbType(mySqlDbType);

        dbType.Should().Be(expectedDbType);
    }

    [Theory]
    [InlineData("json", DbType.String, MySqlDbType.JSON, typeof(string), true, false)]
    [InlineData("varchar", DbType.String, MySqlDbType.VarChar, typeof(string), true, false)]
    [InlineData("geometry", DbType.Object, MySqlDbType.Geometry, typeof(byte[]), null, null)]
    public void WhenMappingNativeTypeThenExpectedMetadataIsReturned(
        string typeName,
        DbType expectedDbType,
        MySqlDbType expectedMySqlDbType,
        Type expectedSystemType,
        bool? expectedIsUnicode,
        bool? expectedIsFixedLength)
    {
        var mapping = MySqlTypeMapper.MapNativeType(typeName);

        mapping.DbType.Should().Be(expectedDbType);
        mapping.MySqlDbType.Should().Be(expectedMySqlDbType);
        mapping.SystemType.Should().Be(expectedSystemType);
        mapping.IsUnicode.Should().Be(expectedIsUnicode);
        mapping.IsFixedLength.Should().Be(expectedIsFixedLength);
    }

    [Fact]
    public void WhenMappingUnsupportedDbTypeThenJsonIsReturned()
    {
        var mySqlDbType = MySqlTypeMapper.ToMySqlDbType(DbType.Xml);

        mySqlDbType.Should().Be(MySqlDbType.JSON);
    }
}
