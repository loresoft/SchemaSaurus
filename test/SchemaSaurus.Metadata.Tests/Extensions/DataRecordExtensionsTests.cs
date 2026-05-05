using System.Data;
using System.Data.Common;

using SchemaSaurus.Metadata.Extensions;

namespace SchemaSaurus.Metadata.Tests.Extensions;

public class DataRecordExtensionsTests
{
    [Fact]
    public void WhenTypedValueReadThenValueReturned()
    {
        using var reader = CreateReader(
            typeof(bool), true,
            typeof(byte), (byte)42,
            typeof(char), 'A',
            typeof(DateTime), new DateTime(2024, 1, 2),
            typeof(decimal), 12.34m,
            typeof(double), 56.78d,
            typeof(float), 9.1f,
            typeof(Guid), Guid.Parse("9e87f7be-8a52-4e5f-adde-2c487877a923"),
            typeof(short), (short)7,
            typeof(int), 8,
            typeof(long), 9L,
            typeof(string), "value");

        reader.GetBooleanNull(0).Should().BeTrue();
        reader.GetByteNull(1).Should().Be(42);
        reader.GetCharNull(2).Should().Be('A');
        reader.GetDateTimeNull(3).Should().Be(new DateTime(2024, 1, 2));
        reader.GetDecimalNull(4).Should().Be(12.34m);
        reader.GetDoubleNull(5).Should().Be(56.78d);
        reader.GetFloatNull(6).Should().Be(9.1f);
        reader.GetGuidNull(7).Should().Be(Guid.Parse("9e87f7be-8a52-4e5f-adde-2c487877a923"));
        reader.GetInt16Null(8).Should().Be(7);
        reader.GetInt32Null(9).Should().Be(8);
        reader.GetInt64Null(10).Should().Be(9L);
        reader.GetStringNull(11).Should().Be("value");
        reader.GetValueNull(11).Should().Be("value");
    }

    [Fact]
    public void WhenTypedDbNullReadThenNullReturned()
    {
        using var reader = CreateReader(
            typeof(bool), DBNull.Value,
            typeof(byte), DBNull.Value,
            typeof(char), DBNull.Value,
            typeof(DateTime), DBNull.Value,
            typeof(decimal), DBNull.Value,
            typeof(double), DBNull.Value,
            typeof(float), DBNull.Value,
            typeof(Guid), DBNull.Value,
            typeof(short), DBNull.Value,
            typeof(int), DBNull.Value,
            typeof(long), DBNull.Value,
            typeof(string), DBNull.Value,
            typeof(object), DBNull.Value);

        reader.GetBooleanNull(0).Should().BeNull();
        reader.GetByteNull(1).Should().BeNull();
        reader.GetCharNull(2).Should().BeNull();
        reader.GetDateTimeNull(3).Should().BeNull();
        reader.GetDecimalNull(4).Should().BeNull();
        reader.GetDoubleNull(5).Should().BeNull();
        reader.GetFloatNull(6).Should().BeNull();
        reader.GetGuidNull(7).Should().BeNull();
        reader.GetInt16Null(8).Should().BeNull();
        reader.GetInt32Null(9).Should().BeNull();
        reader.GetInt64Null(10).Should().BeNull();
        reader.GetStringNull(11).Should().BeNull();
        reader.GetValueNull(12).Should().BeNull();
    }

    [Fact]
    public void WhenBytesValueReadThenBytesCopiedAndLengthReturned()
    {
        using var reader = CreateReader(typeof(byte[]), new byte[] { 1, 2, 3, 4 });
        var buffer = new byte[2];

        var result = reader.GetBytesNull(0, 1, buffer, 0, buffer.Length);

        result.Should().Be(2);
        buffer.Should().Equal(2, 3);
    }

    [Fact]
    public void WhenBytesDbNullReadThenNullReturned()
    {
        using var reader = CreateReader(typeof(byte[]), DBNull.Value);
        var buffer = new byte[2];

        var result = reader.GetBytesNull(0, 0, buffer, 0, buffer.Length);

        result.Should().BeNull();
    }

    [Fact]
    public void WhenCharsValueReadThenCharsCopiedAndLengthReturned()
    {
        using var reader = CreateReader(typeof(char[]), new[] { 'a', 'b', 'c', 'd' });
        var buffer = new char[2];

        var result = reader.GetCharsNull(0, 1, buffer, 0, buffer.Length);

        result.Should().Be(2);
        buffer.Should().Equal('b', 'c');
    }

    [Fact]
    public void WhenCharsDbNullReadThenNullReturned()
    {
        using var reader = CreateReader(typeof(string), DBNull.Value);
        var buffer = new char[2];

        var result = reader.GetCharsNull(0, 0, buffer, 0, buffer.Length);

        result.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Int32Values))]
    public void WhenValueConvertedToInt32ThenExpectedValueReturned(object? value, int expected)
    {
        using var reader = CreateReader(typeof(object), value ?? DBNull.Value);

        var result = reader.GetValueInt32(0);

        result.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NullableInt32Values))]
    public void WhenValueConvertedToNullableInt32ThenExpectedValueReturned(object? value, int? expected)
    {
        using var reader = CreateReader(typeof(object), value ?? DBNull.Value);

        var result = reader.GetValueInt32Null(0);

        result.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(Int64Values))]
    public void WhenValueConvertedToInt64ThenExpectedValueReturned(object? value, long expected)
    {
        using var reader = CreateReader(typeof(object), value ?? DBNull.Value);

        var result = reader.GetValueInt64(0);

        result.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NullableInt64Values))]
    public void WhenValueConvertedToNullableInt64ThenExpectedValueReturned(object? value, long? expected)
    {
        using var reader = CreateReader(typeof(object), value ?? DBNull.Value);

        var result = reader.GetValueInt64Null(0);

        result.Should().Be(expected);
    }

    [Fact]
    public void WhenDecimalValueConvertedToInt64ThenValueReturned()
    {
        using var reader = CreateReader(typeof(object), 123m);

        var result = reader.GetValueInt64(0);

        result.Should().Be(123L);
    }

    [Fact]
    public void WhenDecimalValueConvertedToNullableInt64ThenValueReturned()
    {
        using var reader = CreateReader(typeof(object), 123m);

        var result = reader.GetValueInt64Null(0);

        result.Should().Be(123L);
    }

    [Fact]
    public void WhenFieldValueReadThenValueReturned()
    {
        using var reader = CreateReader(typeof(string), "value");

        var result = reader.GetFieldValueNull<string>(0);

        result.Should().Be("value");
    }

    [Fact]
    public void WhenFieldValueDbNullReadThenDefaultReturned()
    {
        using var reader = CreateReader(typeof(string), DBNull.Value);

        var result = reader.GetFieldValueNull<string>(0);

        result.Should().BeNull();
    }

    public static TheoryData<object?, int> Int32Values()
        => new()
        {
            { null, 0 },
            { DBNull.Value, 0 },
            { 123, 123 },
            { 123L, 123 },
            { 2147483652L, int.MaxValue },
            { -2147483653L, int.MinValue },
            { "456", 456 },
        };

    public static TheoryData<object?, int?> NullableInt32Values()
        => new()
        {
            { null, null },
            { DBNull.Value, null },
            { 123, 123 },
            { 123L, 123 },
            { 2147483652L, null },
            { -2147483653L, null },
            { "456", 456 },
        };

    public static TheoryData<object?, long> Int64Values()
        => new()
        {
            { null, 0L },
            { DBNull.Value, 0L },
            { 123L, 123L },
            { 123, 123L },
            { "456", 456L },
        };

    public static TheoryData<object?, long?> NullableInt64Values()
        => new()
        {
            { null, null },
            { DBNull.Value, null },
            { 123L, 123L },
            { 123, 123L },
            { "456", 456L },
        };

    private static DbDataReader CreateReader(params object?[] typeAndValuePairs)
    {
        var table = new DataTable();
        var values = new object?[typeAndValuePairs.Length / 2];

        for (var pairIndex = 0; pairIndex < typeAndValuePairs.Length; pairIndex += 2)
        {
            var columnType = (Type)typeAndValuePairs[pairIndex]!;
            var value = typeAndValuePairs[pairIndex + 1];
            var columnOrdinal = pairIndex / 2;
            table.Columns.Add("Column" + columnOrdinal, columnType);
            values[columnOrdinal] = value;
        }

        table.Rows.Add(values);
        var reader = table.CreateDataReader();
        reader.Read();

        return reader;
    }
}
