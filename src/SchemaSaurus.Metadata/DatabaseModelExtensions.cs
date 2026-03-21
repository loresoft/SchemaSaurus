using System.Text.Json;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Extension methods for <see cref="DatabaseModel"/>.
/// </summary>
/// <remarks>
/// Provides three capability groups:
/// <list type="bullet">
///   <item><description>Lookup — case-insensitive find methods for every metadata object type.</description></item>
///   <item><description>Serialization — <see cref="ToJson"/> / <see cref="FromJson"/> round-tripping via <see cref="System.Text.Json"/>.</description></item>
///   <item><description>Wiring — <see cref="ResolveReferences"/> to populate back-reference and resolved-column properties after construction or deserialization.</description></item>
/// </list>
/// </remarks>
public static class DatabaseModelExtensions
{
    /// <summary>
    /// Finds a table by schema and name using <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="schema">Schema name to match, or <see langword="null"/> to match any schema.</param>
    /// <param name="name">Table name to match.</param>
    /// <returns>The matching <see cref="Table"/>, or <see langword="null"/> if not found.</returns>
    public static Table? FindTable(this DatabaseModel model, string? schema, string name)
    {
        return model.Tables.FirstOrDefault(t =>
            (schema is null || string.Equals(t.SchemaQualifiedName.Schema, schema, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(t.SchemaQualifiedName.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a table by <see cref="SchemaQualifiedName"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Schema-qualified name to match.</param>
    /// <returns>The matching <see cref="Table"/>, or <see langword="null"/> if not found.</returns>
    public static Table? FindTable(this DatabaseModel model, SchemaQualifiedName name)
        => model.FindTable(name.Schema, name.Name);

    /// <summary>
    /// Finds a table by name only, matching any schema.
    /// Convenience overload for schema-less providers (e.g., SQLite) or when the
    /// schema is not known.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Table name to match.</param>
    /// <returns>The matching <see cref="Table"/>, or <see langword="null"/> if not found.</returns>
    public static Table? FindTable(this DatabaseModel model, string name)
        => model.FindTable(null, name);

    /// <summary>
    /// Finds a view by schema and name using <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="schema">Schema name to match, or <see langword="null"/> to match any schema.</param>
    /// <param name="name">View name to match.</param>
    /// <returns>The matching <see cref="View"/>, or <see langword="null"/> if not found.</returns>
    public static View? FindView(this DatabaseModel model, string? schema, string name)
    {
        return model.Views.FirstOrDefault(v =>
            (schema is null || string.Equals(v.SchemaQualifiedName.Schema, schema, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(v.SchemaQualifiedName.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a view by <see cref="SchemaQualifiedName"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Schema-qualified name to match.</param>
    /// <returns>The matching <see cref="View"/>, or <see langword="null"/> if not found.</returns>
    public static View? FindView(this DatabaseModel model, SchemaQualifiedName name)
        => model.FindView(name.Schema, name.Name);

    /// <summary>
    /// Finds a view by name only, matching any schema.
    /// Convenience overload for schema-less providers (e.g., SQLite) or when the
    /// schema is not known.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">View name to match.</param>
    /// <returns>The matching <see cref="View"/>, or <see langword="null"/> if not found.</returns>
    public static View? FindView(this DatabaseModel model, string name)
        => model.FindView(null, name);

    /// <summary>
    /// Finds a sequence by schema and name using <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="schema">Schema name to match, or <see langword="null"/> to match any schema.</param>
    /// <param name="name">Sequence name to match.</param>
    /// <returns>The matching <see cref="Sequence"/>, or <see langword="null"/> if not found.</returns>
    public static Sequence? FindSequence(this DatabaseModel model, string? schema, string name)
    {
        return model.Sequences.FirstOrDefault(s =>
            (schema is null || string.Equals(s.SchemaQualifiedName.Schema, schema, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(s.SchemaQualifiedName.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a sequence by <see cref="SchemaQualifiedName"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Schema-qualified name to match.</param>
    /// <returns>The matching <see cref="Sequence"/>, or <see langword="null"/> if not found.</returns>
    public static Sequence? FindSequence(this DatabaseModel model, SchemaQualifiedName name)
        => model.FindSequence(name.Schema, name.Name);

    /// <summary>
    /// Finds a sequence by name only, matching any schema.
    /// Convenience overload for schema-less providers (e.g., SQLite) or when the
    /// schema is not known.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Sequence name to match.</param>
    /// <returns>The matching <see cref="Sequence"/>, or <see langword="null"/> if not found.</returns>
    public static Sequence? FindSequence(this DatabaseModel model, string name)
        => model.FindSequence(null, name);

    /// <summary>
    /// Finds a stored procedure by schema and name using <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="schema">Schema name to match, or <see langword="null"/> to match any schema.</param>
    /// <param name="name">Stored procedure name to match.</param>
    /// <returns>The matching <see cref="StoredProcedure"/>, or <see langword="null"/> if not found.</returns>
    public static StoredProcedure? FindStoredProcedure(this DatabaseModel model, string? schema, string name)
    {
        return model.StoredProcedures.FirstOrDefault(sp =>
            (schema is null || string.Equals(sp.SchemaQualifiedName.Schema, schema, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(sp.SchemaQualifiedName.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a stored procedure by <see cref="SchemaQualifiedName"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Schema-qualified name to match.</param>
    /// <returns>The matching <see cref="StoredProcedure"/>, or <see langword="null"/> if not found.</returns>
    public static StoredProcedure? FindStoredProcedure(this DatabaseModel model, SchemaQualifiedName name)
        => model.FindStoredProcedure(name.Schema, name.Name);

    /// <summary>
    /// Finds a stored procedure by name only, matching any schema.
    /// Convenience overload for schema-less providers (e.g., SQLite) or when the
    /// schema is not known.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Stored procedure name to match.</param>
    /// <returns>The matching <see cref="StoredProcedure"/>, or <see langword="null"/> if not found.</returns>
    public static StoredProcedure? FindStoredProcedure(this DatabaseModel model, string name)
        => model.FindStoredProcedure(null, name);

    /// <summary>
    /// Finds a scalar function by schema and name using <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="schema">Schema name to match, or <see langword="null"/> to match any schema.</param>
    /// <param name="name">Function name to match.</param>
    /// <returns>The matching <see cref="ScalarFunction"/>, or <see langword="null"/> if not found.</returns>
    public static ScalarFunction? FindScalarFunction(this DatabaseModel model, string? schema, string name)
    {
        return model.ScalarFunctions.FirstOrDefault(f =>
            (schema is null || string.Equals(f.SchemaQualifiedName.Schema, schema, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(f.SchemaQualifiedName.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a scalar function by <see cref="SchemaQualifiedName"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Schema-qualified name to match.</param>
    /// <returns>The matching <see cref="ScalarFunction"/>, or <see langword="null"/> if not found.</returns>
    public static ScalarFunction? FindScalarFunction(this DatabaseModel model, SchemaQualifiedName name)
        => model.FindScalarFunction(name.Schema, name.Name);

    /// <summary>
    /// Finds a scalar function by name only, matching any schema.
    /// Convenience overload for schema-less providers (e.g., SQLite) or when the
    /// schema is not known.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Function name to match.</param>
    /// <returns>The matching <see cref="ScalarFunction"/>, or <see langword="null"/> if not found.</returns>
    public static ScalarFunction? FindScalarFunction(this DatabaseModel model, string name)
        => model.FindScalarFunction(null, name);

    /// <summary>
    /// Finds a table-valued function by schema and name using <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="schema">Schema name to match, or <see langword="null"/> to match any schema.</param>
    /// <param name="name">Function name to match.</param>
    /// <returns>The matching <see cref="TableValuedFunction"/>, or <see langword="null"/> if not found.</returns>
    public static TableValuedFunction? FindTableValuedFunction(this DatabaseModel model, string? schema, string name)
    {
        return model.TableValuedFunctions.FirstOrDefault(f =>
            (schema is null || string.Equals(f.SchemaQualifiedName.Schema, schema, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(f.SchemaQualifiedName.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a table-valued function by <see cref="SchemaQualifiedName"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Schema-qualified name to match.</param>
    /// <returns>The matching <see cref="TableValuedFunction"/>, or <see langword="null"/> if not found.</returns>
    public static TableValuedFunction? FindTableValuedFunction(this DatabaseModel model, SchemaQualifiedName name)
        => model.FindTableValuedFunction(name.Schema, name.Name);

    /// <summary>
    /// Finds a table-valued function by name only, matching any schema.
    /// Convenience overload for schema-less providers (e.g., SQLite) or when the
    /// schema is not known.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Function name to match.</param>
    /// <returns>The matching <see cref="TableValuedFunction"/>, or <see langword="null"/> if not found.</returns>
    public static TableValuedFunction? FindTableValuedFunction(this DatabaseModel model, string name)
        => model.FindTableValuedFunction(null, name);

    /// <summary>
    /// Finds a user-defined type by schema and name using <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="schema">Schema name to match, or <see langword="null"/> to match any schema.</param>
    /// <param name="name">Type name to match.</param>
    /// <returns>The matching <see cref="UserDefinedType"/>, or <see langword="null"/> if not found.</returns>
    public static UserDefinedType? FindUserDefinedType(this DatabaseModel model, string? schema, string name)
    {
        return model.UserDefinedTypes.FirstOrDefault(t =>
            (schema is null || string.Equals(t.SchemaQualifiedName.Schema, schema, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(t.SchemaQualifiedName.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a user-defined type by <see cref="SchemaQualifiedName"/>.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Schema-qualified name to match.</param>
    /// <returns>The matching <see cref="UserDefinedType"/>, or <see langword="null"/> if not found.</returns>
    public static UserDefinedType? FindUserDefinedType(this DatabaseModel model, SchemaQualifiedName name)
        => model.FindUserDefinedType(name.Schema, name.Name);

    /// <summary>
    /// Finds a user-defined type by name only, matching any schema.
    /// Convenience overload for schema-less providers (e.g., SQLite) or when the
    /// schema is not known.
    /// </summary>
    /// <param name="model">The database model to search.</param>
    /// <param name="name">Type name to match.</param>
    /// <returns>The matching <see cref="UserDefinedType"/>, or <see langword="null"/> if not found.</returns>
    public static UserDefinedType? FindUserDefinedType(this DatabaseModel model, string name)
        => model.FindUserDefinedType(null, name);


    /// <summary>
    /// Wires all parent back-references and resolved column references throughout the
    /// object graph so that navigation properties like <see cref="RelationBase.Database"/>,
    /// <see cref="Column.Parent"/>, and <see cref="ColumnReference.Column"/> are usable.
    /// </summary>
    /// <remarks>
    /// Called automatically by <see cref="Builders.DatabaseModelBuilder.Build"/> and by
    /// <see cref="FromJson"/> after deserialization. Safe to call multiple times; each
    /// invocation overwrites previously wired references.
    /// </remarks>
    /// <param name="model">The database model whose back-references should be wired.</param>
    public static void ResolveReferences(this DatabaseModel model)
    {
        foreach (var table in model.Tables)
        {
            table.Database = model;
            WireRelationBase(table);
            WireTable(model, table);
        }

        foreach (var view in model.Views)
        {
            view.Database = model;
            WireRelationBase(view);
        }
    }

    private static void WireRelationBase(RelationBase relation)
    {
        foreach (var column in relation.Columns)
            column.Parent = relation;

        foreach (var index in relation.Indexes)
        {
            foreach (var col in index.Columns)
                ResolveColumnRef(col, relation);
        }
    }

    private static void WireTable(DatabaseModel model, Table table)
    {
        if (table.PrimaryKey is { } pk)
        {
            foreach (var col in pk.Columns)
                ResolveColumnRef(col, table);
        }

        foreach (var uc in table.UniqueConstraints)
        {
            foreach (var col in uc.Columns)
                ResolveColumnRef(col, table);
        }

        foreach (var fk in table.ForeignKeys)
        {
            fk.DependentTable = table;

            var principalTable = model.FindTable(fk.PrincipalTableName);
            if (principalTable is not null)
            {
                fk.PrincipalTable = principalTable;
                foreach (var mapping in fk.ColumnMappings)
                {
                    mapping.DependentColumn = table.Columns
                        .FirstOrDefault(c => string.Equals(
                            c.Name, mapping.DependentColumnName,
                            StringComparison.OrdinalIgnoreCase))!;

                    mapping.PrincipalColumn = principalTable.Columns
                        .FirstOrDefault(c => string.Equals(
                            c.Name, mapping.PrincipalColumnName,
                            StringComparison.OrdinalIgnoreCase))!;
                }
            }
        }
    }

    private static void ResolveColumnRef(ColumnReference colRef, RelationBase relation)
    {
        colRef.Column = relation.Columns
            .FirstOrDefault(c => string.Equals(
                c.Name, colRef.ColumnName,
                StringComparison.OrdinalIgnoreCase))!;
    }


    /// <summary>
    /// Serializes a <see cref="DatabaseModel"/> to a JSON string using the pre-configured
    /// <see cref="MetadataJsonContext"/> converter set.
    /// </summary>
    /// <param name="model">The database model to serialize.</param>
    /// <returns>A JSON string representing the full metadata snapshot.</returns>
    public static string ToJson(this DatabaseModel model)
        => JsonSerializer.Serialize(model, MetadataJsonContext.JsonSerializerOptions.Value);

    /// <summary>
    /// Deserializes a <see cref="DatabaseModel"/> from a JSON string produced by
    /// <see cref="ToJson"/>.
    /// </summary>
    /// <remarks>
    /// Automatically calls <see cref="ResolveReferences"/> after deserialization so
    /// that all navigation properties are fully populated before the model is returned.
    /// </remarks>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A fully navigable <see cref="DatabaseModel"/> instance with all
    /// back-references wired.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when deserialization produces a <see langword="null"/> result.
    /// </exception>
    public static DatabaseModel FromJson(string json)
    {
        var model = JsonSerializer.Deserialize<DatabaseModel>(json, MetadataJsonContext.JsonSerializerOptions.Value)
            ?? throw new InvalidOperationException(
                "Deserialization of DatabaseModel returned null. " +
                "Ensure the JSON represents a valid non-null object.");

        model.ResolveReferences();
        return model;
    }
}
