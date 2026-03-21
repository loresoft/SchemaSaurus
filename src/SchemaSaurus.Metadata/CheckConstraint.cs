using System.Diagnostics;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a check constraint defined on a <see cref="Table"/>.
/// </summary>
/// <remarks>
/// A check constraint enforces a domain-level validation rule by requiring every row
/// to satisfy a Boolean expression evaluated at insert and update time.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class CheckConstraint
{
    /// <summary>
    /// Catalog-level name that uniquely identifies this check constraint within its parent table.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The raw SQL Boolean expression that every row must satisfy,
    /// exactly as stored in the database catalog
    /// (e.g., <c>"([Quantity] &gt; 0)"</c>).
    /// </summary>
    [JsonPropertyName("expression")]
    public required string Expression { get; init; }
}
