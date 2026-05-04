# Copilot Instructions

## Project Guidelines
- When naming ordinal variables in code, use the full suffix "Ordinal" instead of the abbreviation "Ord".
- Avoid multiple statements or object creation inside method calls on one line. Prefer assigning constructed objects to a local variable before passing them to methods; for example, create a ColumnReference variable before calling kc.Columns.Add(reference).

## Data Access Guidelines
- When fixing SequentialAccess issues, preserve the reader CommandBehavior and reorder data-reader reads instead of changing reader options.
