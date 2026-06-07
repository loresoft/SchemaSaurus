using System.Data;

using Oracle.ManagedDataAccess.Client;

namespace SchemaSaurus.Oracle.Tests;

public class OracleDataTypeMapperTests
{
    [Theory]
    [InlineData("SDO_GEOMETRY", DbType.Object, OracleDbType.Object, typeof(object), null, null)]
    [InlineData("MDSYS.SDO_GEOMETRY", DbType.Object, OracleDbType.Object, typeof(object), null, null)]
    public void WhenMappingNativeTypeThenExpectedMetadataIsReturned(
        string typeName,
        DbType expectedDbType,
        OracleDbType expectedOracleDbType,
        Type expectedSystemType,
        bool? expectedIsUnicode,
        bool? expectedIsFixedLength)
    {
        var mapping = OracleTypeMapper.MapNativeType(typeName);

        mapping.DbType.Should().Be(expectedDbType);
        mapping.OracleDbType.Should().Be(expectedOracleDbType);
        mapping.SystemType.Should().Be(expectedSystemType);
        mapping.IsUnicode.Should().Be(expectedIsUnicode);
        mapping.IsFixedLength.Should().Be(expectedIsFixedLength);
    }
}
