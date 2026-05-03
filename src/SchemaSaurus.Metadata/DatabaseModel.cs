using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// The root object representing a complete, immutable snapshot of a database's structural metadata.
/// </summary>
/// <remarks>
/// All object collections are flat — owned directly by this class, not nested under a schema
/// container. Each object carries its own <see cref="SchemaQualifiedName"/> so consumers can
/// group or filter by schema without requiring a hierarchical object graph.
/// Instances are designed for JSON round-tripping and structural equality comparison.
/// </remarks>
[Equatable]
[DebuggerDisplay("{DatabaseName}")]
public sealed partial class DatabaseModel : IAnnotatable
{
    /// <summary>The name of the database this snapshot was captured from.</summary>
    [JsonPropertyName("databaseName")]
    public required string DatabaseName { get; init; }

    /// <summary>
    /// Default collation name for the database
    /// (e.g., <c>"SQL_Latin1_General_CP1_CI_AS"</c> for SQL Server,
    /// <c>"en_US.utf8"</c> for PostgreSQL).
    /// <see langword="null"/> when the provider does not expose collation information.
    /// </summary>
    [JsonPropertyName("collation")]
    public string? Collation { get; init; }

    /// <summary>
    /// Database engine edition or product variant (e.g., <c>"Enterprise Edition"</c>, <c>"Azure SQL Database"</c>).
    /// <see langword="null"/> when the provider does not expose edition information.
    /// </summary>
    [JsonPropertyName("edition")]
    public string? Edition { get; init; }

    /// <summary>
    /// Database engine edition family or deployment type (e.g., <c>"Enterprise"</c>, <c>"AzureSQLDatabase"</c>).
    /// <see langword="null"/> when the provider does not expose engine edition information.
    /// </summary>
    [JsonPropertyName("engineEdition")]
    public string? EngineEdition { get; init; }

    /// <summary>
    /// Database compatibility level as reported by the engine (e.g., <c>"160"</c> for SQL Server 2022).
    /// <see langword="null"/> when the provider does not expose compatibility level information.
    /// </summary>
    [JsonPropertyName("compatibilityLevel")]
    public string? CompatibilityLevel { get; init; }

    /// <summary>
    /// Default schema name (e.g., <c>"dbo"</c> for SQL Server, <c>"public"</c> for PostgreSQL).
    /// <see langword="null"/> for providers that do not have a default schema concept (e.g., SQLite).
    /// </summary>
    [JsonPropertyName("defaultSchemaName")]
    public string? DefaultSchemaName { get; init; }

    /// <summary>
    /// Identifies the database engine that produced this snapshot
    /// (e.g., <c>"SqlServer"</c>, <c>"PostgreSQL"</c>).
    /// </summary>
    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    /// <summary>
    /// Server version string as reported by the engine
    /// (e.g., <c>"16.0.1135.2"</c> for SQL Server, <c>"16.4"</c> for PostgreSQL).
    /// <see langword="null"/> when the provider does not expose version information.
    /// </summary>
    [JsonPropertyName("serverVersion")]
    public string? ServerVersion { get; init; }

    /// <summary>
    /// All base tables in the database, including columns, indexes, keys, and constraints.
    /// Defaults to an empty collection when the database contains no tables.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("tables")]
    public IReadOnlyList<Table> Tables { get; init; } = [];

    /// <summary>
    /// All views in the database, including standard and materialized/indexed views.
    /// Defaults to an empty collection when the database contains no views.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("views")]
    public IReadOnlyList<View> Views { get; init; } = [];

    /// <summary>
    /// All sequence objects used to generate sequential numeric values.
    /// Defaults to an empty collection when the database contains no sequences.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("sequences")]
    public IReadOnlyList<Sequence> Sequences { get; init; } = [];

    /// <summary>
    /// All stored procedures in the database, including their parameter signatures.
    /// Defaults to an empty collection when the database contains no stored procedures.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("storedProcedures")]
    public IReadOnlyList<StoredProcedure> StoredProcedures { get; init; } = [];

    /// <summary>
    /// All scalar-valued user-defined functions in the database.
    /// Defaults to an empty collection when the database contains no scalar functions.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("scalarFunctions")]
    public IReadOnlyList<ScalarFunction> ScalarFunctions { get; init; } = [];

    /// <summary>
    /// All table-valued user-defined functions in the database.
    /// Defaults to an empty collection when the database contains no table-valued functions.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("tableValuedFunctions")]
    public IReadOnlyList<TableValuedFunction> TableValuedFunctions { get; init; } = [];

    /// <summary>
    /// All user-defined types (aliases, table types, CLR types) in the database.
    /// Defaults to an empty collection when the database contains no user-defined types.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("userDefinedTypes")]
    public IReadOnlyList<UserDefinedType> UserDefinedTypes { get; init; } = [];

    /// <inheritdoc />
    [DictionaryEquality]
    [JsonPropertyName("annotations")]
    [JsonConverter(typeof(ReadOnlyDictionaryConverter<string, object?>))]
    public IReadOnlyDictionary<string, object?> Annotations { get; init; } = new Dictionary<string, object?>();
}
