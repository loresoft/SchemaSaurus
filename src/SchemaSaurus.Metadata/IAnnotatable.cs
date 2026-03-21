namespace SchemaSaurus.Metadata;

/// <summary>
/// Implemented by every metadata object that supports provider-specific extension data.
/// </summary>
/// <remarks>
/// Annotation keys should follow a namespaced convention to avoid collisions across providers:
/// <list type="bullet">
///   <item><description><c>"SqlServer:FileGroup"</c></description></item>
///   <item><description><c>"SqlServer:IsMemoryOptimized"</c></description></item>
///   <item><description><c>"PostgreSql:Tablespace"</c></description></item>
/// </list>
/// SQL Server <c>MS_Description</c> extended properties are surfaced into the
/// <c>Description</c> property on the owning type rather than stored as annotations.
/// </remarks>
public interface IAnnotatable
{
    /// <summary>
    /// Provider-specific annotation values keyed by namespaced string identifiers.
    /// Values may be any JSON-serializable type; when round-tripped through JSON they
    /// are represented as <see cref="System.Text.Json.JsonElement"/> instances.
    /// Values may be <see langword="null"/>.
    /// </summary>
    IReadOnlyDictionary<string, object?> Annotations { get; }
}
