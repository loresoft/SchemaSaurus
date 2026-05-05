namespace SchemaSaurus.Oracle;

/// <summary>
/// Provides well-known Oracle annotation names used by SchemaSaurus metadata.
/// </summary>
public static class OracleAnnotations
{
    /// <summary>
    /// Annotation containing the Oracle-specific OracleDbType name for a mapped type.
    /// </summary>
    public const string OracleDbType = "OracleDbType";

    /// <summary>
    /// Annotation containing the Oracle index-organized table type.
    /// </summary>
    public const string IotType = "IotType";

    /// <summary>
    /// Annotation indicating whether an Oracle table is temporary.
    /// </summary>
    public const string Temporary = "Temporary";

    /// <summary>
    /// Annotation indicating whether an Oracle sequence is ordered.
    /// </summary>
    public const string SequenceOrder = "SequenceOrder";

    /// <summary>
    /// Annotation containing an Oracle user-defined type category.
    /// </summary>
    public const string TypeCode = "TypeCode";
}
