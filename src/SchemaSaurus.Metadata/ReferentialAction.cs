namespace SchemaSaurus.Metadata;

/// <summary>
/// Referential integrity action applied to dependent rows when the principal row
/// is deleted or updated.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReferentialAction>))]
public enum ReferentialAction
{
    /// <summary>No automatic action; the operation will fail if dependent rows exist.</summary>
    NoAction,

    /// <summary>Automatically delete or update dependent rows.</summary>
    Cascade,

    /// <summary>Set the foreign key columns in dependent rows to NULL.</summary>
    SetNull,

    /// <summary>Set the foreign key columns in dependent rows to their default values.</summary>
    SetDefault,

    /// <summary>Prevent the operation if any dependent rows exist (equivalent to NoAction on most providers).</summary>
    Restrict,
}
