namespace SchemaSaurus.Metadata;

/// <summary>
/// Structural kind of a user-defined type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserDefinedTypeKind>))]
public enum UserDefinedTypeKind
{
    /// <summary>A type alias over a built-in type (CREATE TYPE … FROM …).</summary>
    Alias,

    /// <summary>A table-valued parameter type (SQL Server table type).</summary>
    TableType,

    /// <summary>A composite (row) type (PostgreSQL).</summary>
    Composite,

    /// <summary>An enumeration type (PostgreSQL CREATE TYPE … AS ENUM).</summary>
    Enum,

    /// <summary>A domain type with constraints (PostgreSQL CREATE DOMAIN).</summary>
    Domain,
}
