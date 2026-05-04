using System.Data.Common;

namespace SchemaSaurus.Metadata.Extensions;

public static class DataRecordExtensions
{
    public static bool? GetBooleanNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetBoolean(ordinal);

    public static byte? GetByteNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetByte(ordinal);

    public static long? GetBytesNull(this IDataRecord dataRecord, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetBytes(ordinal, fieldOffset, buffer, bufferOffset, length);

    public static char? GetCharNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetChar(ordinal);

    public static long? GetCharsNull(this IDataRecord dataRecord, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetChars(ordinal, fieldOffset, buffer, bufferOffset, length);

    public static DateTime? GetDateTimeNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetDateTime(ordinal);

    public static decimal? GetDecimalNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetDecimal(ordinal);

    public static double? GetDoubleNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetDouble(ordinal);

    public static float? GetFloatNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetFloat(ordinal);

    public static Guid? GetGuidNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetGuid(ordinal);

    public static short? GetInt16Null(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetInt16(ordinal);

    public static int? GetInt32Null(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetInt32(ordinal);

    public static long? GetInt64Null(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetInt64(ordinal);

    public static string? GetStringNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetString(ordinal);

    public static object? GetValueNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetValue(ordinal);

    public static T? GetFieldValueNull<T>(this DbDataReader dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? default : dataRecord.GetFieldValue<T>(ordinal);
}
