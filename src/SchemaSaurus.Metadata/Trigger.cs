using System.Diagnostics;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a DML trigger defined on a <see cref="Table"/> or <see cref="View"/>.
/// </summary>
/// <remarks>
/// Triggers are owned by their parent relation and are listed in
/// <see cref="RelationBase.Triggers"/>. A single trigger can respond to one or more
/// DML events (<c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c>) as indicated by
/// <see cref="Events"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class Trigger
{
    /// <summary>
    /// Name of the trigger as defined in the database catalog.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// When the trigger fires relative to the triggering DML statement.
    /// </summary>
    /// <remarks>
    /// See <see cref="TriggerTiming"/> for possible values
    /// (e.g. <c>BEFORE</c>, <c>AFTER</c>, <c>INSTEAD OF</c>).
    /// </remarks>
    [JsonPropertyName("timing")]
    public TriggerTiming Timing { get; init; }

    /// <summary>
    /// One or more DML events that activate this trigger.
    /// </summary>
    /// <remarks>
    /// Multiple events are combined with bitwise OR on <see cref="TriggerEvent"/>
    /// (e.g. <c>INSERT | UPDATE</c>).
    /// </remarks>
    [JsonPropertyName("events")]
    public TriggerEvent Events { get; init; }

    /// <summary>
    /// SQL definition of the trigger.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the caller lacks the necessary permissions to read
    /// the definition, or when the database does not expose trigger bodies.
    /// </remarks>
    [JsonPropertyName("definition")]
    public string? Definition { get; init; }

    /// <summary>
    /// Indicates whether this trigger is currently disabled.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; init; }
}
