namespace SchemaSaurus.Metadata.Extensions;

/// <summary>
/// Extension methods for normalizing type facet values based on <see cref="DbType"/>.
/// </summary>
public static class NormalizeExtensions
{
    /// <summary>
    /// Returns the precision value only when <paramref name="dbType"/> is <see cref="DbType.Decimal"/>;
    /// otherwise returns <see langword="null"/>.
    /// </summary>
    /// <param name="precision">The raw precision value from the data source.</param>
    /// <param name="dbType">The normalized data type.</param>
    /// <returns>The precision if applicable; otherwise <see langword="null"/>.</returns>
    public static int? NormalizePrecision(this int? precision, DbType dbType)
        => dbType == DbType.Decimal ? precision : null;

    /// <summary>
    /// Returns the scale value only when <paramref name="dbType"/> is <see cref="DbType.Decimal"/>;
    /// otherwise returns <see langword="null"/>.
    /// </summary>
    /// <param name="scale">The raw scale value from the data source.</param>
    /// <param name="dbType">The normalized data type.</param>
    /// <returns>The scale if applicable; otherwise <see langword="null"/>.</returns>
    public static int? NormalizeScale(this int? scale, DbType dbType)
        => dbType == DbType.Decimal ? scale : null;
}
