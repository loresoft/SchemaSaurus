namespace SchemaSaurus.Metadata.Extensions;

public static class NormalizeExtensions
{
    public static int? NormalizePrecision(this int? precision, DbType dbType)
        => dbType == DbType.Decimal ? precision : null;

    public static int? NormalizeScale(this int? scale, DbType dbType)
        => dbType == DbType.Decimal ? scale : null;
}
