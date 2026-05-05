using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SchemaSaurus.Metadata.Extensions;

/// <summary>
/// Null-safe extension methods for reading typed values from an <see cref="IDataRecord"/>.
/// Each method returns <see langword="null"/> (or <see langword="default"/>) when the field
/// value is <see cref="DBNull"/>, avoiding manual <see cref="IDataRecord.IsDBNull"/> checks.
/// </summary>
public static class DataRecordExtensions
{
    /// <summary>Returns the <see cref="bool"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The boolean value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static bool? GetBooleanNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetBoolean(ordinal);

    /// <summary>Returns the <see cref="byte"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The byte value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static byte? GetByteNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetByte(ordinal);

    /// <summary>Reads bytes into <paramref name="buffer"/> from the specified ordinal, or returns <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <param name="fieldOffset">The index within the field from which to begin reading.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bufferOffset">The index within <paramref name="buffer"/> at which to begin writing.</param>
    /// <param name="length">The maximum number of bytes to read.</param>
    /// <returns>The number of bytes read, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static long? GetBytesNull(this IDataRecord dataRecord, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetBytes(ordinal, fieldOffset, buffer, bufferOffset, length);

    /// <summary>Returns the <see cref="char"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The character value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static char? GetCharNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetChar(ordinal);

    /// <summary>Reads characters into <paramref name="buffer"/> from the specified ordinal, or returns <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <param name="fieldOffset">The index within the field from which to begin reading.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bufferOffset">The index within <paramref name="buffer"/> at which to begin writing.</param>
    /// <param name="length">The maximum number of characters to read.</param>
    /// <returns>The number of characters read, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static long? GetCharsNull(this IDataRecord dataRecord, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetChars(ordinal, fieldOffset, buffer, bufferOffset, length);

    /// <summary>Returns the <see cref="DateTime"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The date/time value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static DateTime? GetDateTimeNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetDateTime(ordinal);

    /// <summary>Returns the <see cref="decimal"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The decimal value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static decimal? GetDecimalNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetDecimal(ordinal);

    /// <summary>Returns the <see cref="double"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The double value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static double? GetDoubleNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetDouble(ordinal);

    /// <summary>Returns the <see cref="float"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The float value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static float? GetFloatNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetFloat(ordinal);

    /// <summary>Returns the <see cref="Guid"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The GUID value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static Guid? GetGuidNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetGuid(ordinal);

    /// <summary>Returns the <see cref="short"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The 16-bit integer value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static short? GetInt16Null(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetInt16(ordinal);

    /// <summary>Returns the <see cref="int"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The 32-bit integer value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static int? GetInt32Null(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetInt32(ordinal);

    /// <summary>Returns the <see cref="long"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The 64-bit integer value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static long? GetInt64Null(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetInt64(ordinal);

    /// <summary>Returns the <see cref="string"/> value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The string value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static string? GetStringNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetString(ordinal);

    /// <summary>Returns the boxed value at the specified ordinal, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The boxed value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static object? GetValueNull(this IDataRecord dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? null : dataRecord.GetValue(ordinal);


    /// <summary>Returns the value at the specified ordinal converted to <see cref="int"/>, or <c>0</c> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The converted 32-bit integer value, or <c>0</c> when the field is <see cref="DBNull"/>.</returns>
    public static int GetValueInt32(this IDataRecord dataRecord, int ordinal)
    {
        if (dataRecord.IsDBNull(ordinal))
            return default;

        var value = dataRecord.GetValue(ordinal);
        if (value is null or DBNull)
            return default;

        if (value is int intValue)
            return intValue;

        if (value is long longValue)
        {
            if (longValue < int.MinValue)
                return int.MinValue;

            if (longValue > int.MaxValue)
                return int.MaxValue;

            return (int)longValue;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the value at the specified ordinal converted to <see cref="int"/>, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The converted 32-bit integer value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static int? GetValueInt32Null(this IDataRecord dataRecord, int ordinal)
    {
        if (dataRecord.IsDBNull(ordinal))
            return null;

        var value = dataRecord.GetValue(ordinal);
        if (value is null or DBNull)
            return null;

        if (value is int intValue)
            return intValue;

        if (value is long longValue)
            return longValue is > int.MaxValue or < int.MinValue ? null : (int)longValue;

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }


    /// <summary>Returns the value at the specified ordinal converted to <see cref="long"/>, or <c>0</c> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The converted 64-bit integer value, or <c>0</c> when the field is <see cref="DBNull"/>.</returns>
    public static long GetValueInt64(this IDataRecord dataRecord, int ordinal)
    {
        if (dataRecord.IsDBNull(ordinal))
            return default;

        var value = dataRecord.GetValue(ordinal);
        if (value is null or DBNull)
            return default;

        if (value is long longValue)
            return longValue;

        if (value is decimal decimalValue)
        {
            if (decimalValue < long.MinValue)
                return long.MinValue;

            if (decimalValue > long.MaxValue)
                return long.MaxValue;

            return decimal.ToInt64(decimalValue);
        }

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the value at the specified ordinal converted to <see cref="long"/>, or <see langword="null"/> if the field is <see cref="DBNull"/>.</summary>
    /// <param name="dataRecord">The data record to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The converted 64-bit integer value, or <see langword="null"/> when the field is <see cref="DBNull"/>.</returns>
    public static long? GetValueInt64Null(this IDataRecord dataRecord, int ordinal)
    {
        if (dataRecord.IsDBNull(ordinal))
            return null;

        var value = dataRecord.GetValue(ordinal);
        if (value is null or DBNull)
            return null;

        if (value is long longValue)
            return longValue;

        if (value is decimal decimalValue)
            return decimalValue is > long.MaxValue or < long.MinValue ? null : decimal.ToInt64(decimalValue);

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }


    /// <summary>Returns the strongly-typed value at the specified ordinal, or <see langword="default"/> if the field is <see cref="DBNull"/>.</summary>
    /// <typeparam name="T">The target value type.</typeparam>
    /// <param name="dataRecord">The data reader to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The typed field value, or <see langword="default"/> when the field is <see cref="DBNull"/>.</returns>
    public static T? GetFieldValueNull<T>(this DbDataReader dataRecord, int ordinal)
        => dataRecord.IsDBNull(ordinal) ? default : dataRecord.GetFieldValue<T>(ordinal);
}
