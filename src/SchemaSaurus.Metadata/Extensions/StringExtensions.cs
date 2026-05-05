using System.Diagnostics.CodeAnalysis;

namespace SchemaSaurus.Metadata.Extensions;

/// <summary>
/// <see cref="T:String"/> extension methods
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Indicates whether the specified String object is null or an empty string
    /// </summary>
    /// <param name="item">A String reference</param>
    /// <returns>
    ///     <c>true</c> if is null or empty; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? item)
        => string.IsNullOrEmpty(item);

    /// <summary>
    /// Indicates whether a specified string is null, empty, or consists only of white-space characters
    /// </summary>
    /// <param name="item">A String reference</param>
    /// <returns>
    ///      <c>true</c> if is null or empty; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? item)
        => string.IsNullOrWhiteSpace(item);

    /// <summary>
    /// Determines whether the specified string is not <see cref="IsNullOrEmpty"/>.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified <paramref name="value"/> is not <see cref="IsNullOrEmpty"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool HasValue([NotNullWhen(true)] this string? value)
        => !string.IsNullOrEmpty(value);

    /// <summary>
    /// Returns null if the specified string is null or empty; otherwise, returns the original string.
    /// </summary>
    /// <param name="value">The string to check for null or empty.</param>
    /// <returns>The original string if it is not null or empty; otherwise, null.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? NullIfEmpty(this string? value)
        => string.IsNullOrEmpty(value) ? null : value;

    /// <summary>
    /// Escapes a string literal for use in SQL queries.
    /// </summary>
    /// <param name="value">The string to escape, or null to return a SQL null literal.</param>
    /// <param name="literal">The literal delimiter to escape.</param>
    /// <param name="escape">The character to use for escaping the literal.</param>
    /// <returns>The escaped string.</returns>
    public static string EscapeLiteral(this string? value, char literal = '\'', char escape = '\'')
    {
        if (value is null)
            return "NULL";

        var span = value.AsSpan();
        var literalCount = 0;
        foreach (var character in span)
        {
            if (character == literal)
                literalCount++;
        }

        if (literalCount == 0)
            return string.Concat(literal, value, literal);

        var escaped = new char[value.Length + literalCount + 2];

        var escapedIndex = 0;
        escaped[escapedIndex++] = literal;

        foreach (var character in span)
        {
            escaped[escapedIndex++] = character;
            if (character == literal)
                escaped[escapedIndex++] = escape;
        }

        escaped[escapedIndex] = literal;

        return new string(escaped);
    }
}
