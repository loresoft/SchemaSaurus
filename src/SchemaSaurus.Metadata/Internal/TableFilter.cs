namespace SchemaSaurus.Metadata.Internal;

/// <summary>
/// Builds provider filters from <see cref="Provider.SchemaReaderOptions.Tables"/> entries,
/// which may be bare names (<c>"orders"</c>) or schema qualified (<c>"sales.orders"</c>).
/// </summary>
/// <remarks>
/// Entries are split with <see cref="SchemaQualifiedName.Parse"/>. Schema names are carried
/// through to the generated SQL verbatim, so the database decides how they compare;
/// <see cref="IsMatch"/>, which filters in memory, compares ordinal case-insensitively.
/// </remarks>
public static class TableFilter
{
    /// <summary>
    /// Builds a SQL condition that matches the specified table entries, pinning schema
    /// qualified entries to their schema.
    /// </summary>
    /// <param name="tables">The table entries to match; must not be empty.</param>
    /// <param name="schemaExpression">The SQL expression yielding the schema name.</param>
    /// <param name="tableExpression">The SQL expression yielding the object name.</param>
    /// <param name="inClauseBuilder">
    /// Builds a provider specific set membership condition for the specified values and expression.
    /// </param>
    /// <returns>A SQL condition such as <c>(t.name IN ('orders') OR (s.name = 'sales' AND t.name IN ('orders')))</c>.</returns>
    public static string Build(
        IReadOnlyCollection<string> tables,
        string schemaExpression,
        string tableExpression,
        Func<IReadOnlyCollection<string>, string, string> inClauseBuilder)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(inClauseBuilder);

        var unqualified = new List<string>();

        // Schemas are kept in first-seen order so the generated condition is stable across calls.
        var schemas = new List<string>();
        var qualified = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var table in tables)
        {
            var entry = SchemaQualifiedName.Parse(table);
            var schema = entry.Schema;

            if (string.IsNullOrEmpty(schema))
            {
                unqualified.Add(entry.Name);
                continue;
            }

            if (!qualified.TryGetValue(schema, out var names))
            {
                names = [];
                qualified[schema] = names;
                schemas.Add(schema);
            }

            names.Add(entry.Name);
        }

        var conditions = new List<string>(schemas.Count + 1);

        if (unqualified.Count > 0)
            conditions.Add(inClauseBuilder(unqualified, tableExpression));

        foreach (var schema in schemas)
            conditions.Add($"({inClauseBuilder([schema], schemaExpression)} AND {inClauseBuilder(qualified[schema], tableExpression)})");

        return conditions.Count == 1
            ? conditions[0]
            : $"({string.Join(" OR ", conditions)})";
    }

    /// <summary>
    /// Determines whether an object is matched by the specified table entries.
    /// </summary>
    /// <param name="tables">The table entries to match; an empty collection matches everything.</param>
    /// <param name="schema">The schema of the object, or <see langword="null"/> when the provider has none.</param>
    /// <param name="name">The unqualified object name.</param>
    /// <returns><see langword="true"/> when the object is included; otherwise <see langword="false"/>.</returns>
    public static bool IsMatch(IReadOnlyCollection<string> tables, string? schema, string name)
    {
        ArgumentNullException.ThrowIfNull(tables);

        if (tables.Count == 0)
            return true;

        foreach (var table in tables)
        {
            var entry = SchemaQualifiedName.Parse(table);

            if (!string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrEmpty(entry.Schema) || string.Equals(entry.Schema, schema, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
