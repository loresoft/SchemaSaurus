namespace SchemaSaurus.Metadata;

/// <summary>
/// When the trigger fires relative to the triggering DML statement.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TriggerTiming>))]
public enum TriggerTiming
{
    /// <summary>Trigger fires before the DML statement executes.</summary>
    Before,

    /// <summary>Trigger fires after the DML statement executes.</summary>
    After,

    /// <summary>Trigger fires instead of the DML statement (SQL Server / Oracle).</summary>
    InsteadOf,
}
