using System.Data;

namespace SchemaSaurus.Sqlite.Tests;

public class SqliteDataTypeMapperTests
{
    [Theory]
    [InlineData("GEOMETRY")]
    [InlineData("GEOGRAPHY")]
    [InlineData("POINT")]
    [InlineData("LINESTRING")]
    [InlineData("POLYGON")]
    [InlineData("MULTIPOINT")]
    [InlineData("MULTILINESTRING")]
    [InlineData("MULTIPOLYGON")]
    [InlineData("GEOMETRYCOLLECTION")]
    public void WhenMappingSpatialNativeTypeThenObjectMetadataIsReturned(string typeName)
    {
        var mapping = SqliteTypeMapper.MapNativeType(typeName);

        mapping.DbType.Should().Be(DbType.Object);
        mapping.SystemType.Should().Be(typeof(byte[]));
    }

    [Fact]
    public void WhenMappingPointThenIntegerAffinityIsNotUsed()
    {
        var mapping = SqliteTypeMapper.MapNativeType("POINT");

        mapping.DbType.Should().Be(DbType.Object);
    }
}
