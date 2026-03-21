namespace SchemaSaurus.Metadata;

/// <summary>
/// DML events that activate a trigger. Multiple events may be combined with bitwise OR.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<TriggerEvent>))]
public enum TriggerEvent
{
    /// <summary>No events.</summary>
    None = 0,

    /// <summary>Trigger fires on INSERT.</summary>
    Insert = 1,

    /// <summary>Trigger fires on UPDATE.</summary>
    Update = 2,

    /// <summary>Trigger fires on DELETE.</summary>
    Delete = 4,
}
