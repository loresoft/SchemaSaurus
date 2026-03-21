# Prompt: Database Metadata Class Model

## Objective

Design a comprehensive set of C# classes that represent the complete metadata of a relational database. This model must be **provider-agnostic** — it normalizes the structural metadata from SQL Server, PostgreSQL, MySQL, Oracle, and SQLite into a single unified object model.

Think of this as a read-only "schema snapshot" — similar to EF Core's `IModel` / `IRelationalModel` metadata layer, but oriented toward **raw database structure** rather than ORM entity mapping.

---

## Design Constraints

- **Immutable after construction.** Use `init`-only properties or builder patterns. Once the model is built, it should not be mutated.
- **No provider-specific types leak into the model.** Provider differences (e.g., SQL Server `nvarchar` vs PostgreSQL `varchar`) are normalized into a common `DbType` enumeration plus a `NativeTypeName` string that preserves the original provider type for round-tripping.
- **Navigation in both directions.** A `Column` should know its parent `RelationBase` (`Table` or `View`); a `Table`/`View` should know its parent `DatabaseModel`; a `ForeignKey` should reference both the principal and dependent tables/columns.
- **Target .NET 8+.** Use records, required members, collection expressions, and other modern C# features where appropriate.
- **No external dependencies.** Pure POCOs / records. No EF Core, no System.ComponentModel.DataAnnotations.
- **Namespace:** `DatabaseMetadata.Model` (with sub-namespaces if warranted).

---

## Required Top-Level Types

### 1. `DatabaseModel`
The root object representing an entire database. All object collections are **flat** — owned directly by `DatabaseModel`, not nested under a `Schema` container. Each object carries its own `SchemaQualifiedName`. This avoids an artificial schema nesting layer that doesn't map well to providers like SQLite or MySQL, while still preserving schema information on every object.

Must include:
- Database name, collation, default schema name
- Provider identifier — use a `DatabaseProvider` **record struct** (not an enum) containing a `string Name` property. Provide static well-known instances (`DatabaseProvider.SqlServer`, `DatabaseProvider.PostgreSql`, `DatabaseProvider.MySql`, `DatabaseProvider.Oracle`, `DatabaseProvider.Sqlite`) but allow arbitrary values for future/niche providers (e.g., `new DatabaseProvider("CockroachDb")`). The struct should have structural equality based on case-insensitive name comparison.
- Server version string
- **Flat collections** of: `Table`, `View`, `Sequence`, `StoredProcedure`, `ScalarFunction`, `TableValuedFunction`, `UserDefinedType`
- **Computed schema grouping**: `IReadOnlyDictionary<string, SchemaGroup> Schemas` — a convenience lens that groups objects by schema name. `SchemaGroup` is a lightweight read-only projection, not an ownership container. It is built automatically during `DatabaseModelBuilder.Build()`.
- Convenience lookup methods: `FindTable(string schema, string name)`, `FindTable(SchemaQualifiedName name)`, `FindView(...)`, etc.

### 2. `SchemaGroup`
A **read-only grouping projection** — not a first-class ownership container. It provides filtered views into the flat collections on `DatabaseModel`, grouped by schema name.

Must include:
- Schema name
- Read-only filtered collections of `Table`, `View`, `Sequence`, `StoredProcedure`, `ScalarFunction`, `TableValuedFunction`, `UserDefinedType` that belong to this schema
- This type should be a record. It has no `Owner` or back-reference to `DatabaseModel` — it's purely a convenience accessor.

> **Design note:** There is no `Schema` entity that owns objects. Every `Table`, `View`, `Sequence`, etc. carries a `SchemaQualifiedName` property as its identity. The `SchemaGroup` is computed from those names and is not serialized — it is rebuilt on deserialization or after `Build()`.

### 3. `RelationBase` (abstract base class for `Table` and `View`)
`Table` and `View` share significant structure — both have schema-qualified names, columns, indexes, descriptions, and a parent `DatabaseModel` reference. Extract the common members into an abstract base class `RelationBase` to avoid duplication and to allow generic code that operates on "any column-bearing object."

`RelationBase` must include:
- `SchemaQualifiedName` as the primary identity (schema + object name)
- Back-reference to parent `DatabaseModel` (`[JsonIgnore]`)
- Collection of `Column` objects (ordered by ordinal position)
- Collections of: `Index`, `Trigger`
- Description / comment (from `MS_Description` / `COMMENT ON`)
- Annotations (`IAnnotatable`)

### 4. `Table` (extends `RelationBase`)
Adds table-specific members on top of the base:
- `PrimaryKey` (nullable — heap tables have none)
- Collections of: `UniqueConstraint`, `CheckConstraint`, `ForeignKey`
- Table-level properties: `IsTemporalTable`, `TemporalHistoryTableName`, `IsMemoryOptimized`, `IsFileTable` — use an extensible `TableProperties` dictionary or a dedicated `TableOptions` record so provider-specific flags don't pollute the core type
- Row count estimate (nullable `long?`)

### 5. `View` (extends `RelationBase`)
Adds view-specific members on top of the base:
- View definition SQL text (nullable — may not have permission to read it)
- `IsMaterialized` flag (PostgreSQL materialized views, SQL Server indexed views)
- No primary key, unique constraints, check constraints, or foreign keys (views don't own constraints — those live only on `Table`)

### 6. `Column`
Must include:
- Name, ordinal position
- **`TypeMapping`** property (see Supporting Types below) — encapsulates `DbTypeCode`, `NativeTypeName`, `SystemType`, max length, precision, scale, is unicode, is fixed length
- Nullability (`IsNullable`)
- Default value expression (string, raw SQL)
- `IsIdentity` / identity seed & increment
- `IsComputed`, `ComputedColumnSql`, `IsStored` (persisted computed column)
- `IsRowVersion` / `IsConcurrencyToken`
- Collation override (nullable)
- Description / comment
- Back-reference to parent `RelationBase` (typed as the base class so it works for both `Table` and `View`)

### 6. `PrimaryKey`
- Name
- Ordered list of `ColumnReference` (column + sort direction)
- `IsClustered` (SQL Server concept — normalize as bool, default true for SQL Server, ignored elsewhere)

### 7. `UniqueConstraint`
- Name
- Ordered list of `ColumnReference`

### 8. `CheckConstraint`
- Name
- Expression (raw SQL string)

### 9. `ForeignKey`
Must include:
- Constraint name
- **Principal (referenced) table** — schema-qualified reference
- **Dependent (referencing) table** — back-reference to owning table
- Ordered column mappings: list of `(DependentColumn, PrincipalColumn)` pairs
- `OnDelete` action (enum: `NoAction`, `Cascade`, `SetNull`, `SetDefault`, `Restrict`)
- `OnUpdate` action (same enum)
- `IsDisabled` (SQL Server supports disabling constraints)

### 10. `Index`
Must include:
- Name
- Ordered list of `IndexColumn` (column + sort direction + is included column)
- `IsUnique`, `IsClustered`, `IsFiltered`, `FilterExpression`
- Index type enum: `BTree`, `Hash`, `GiST`, `GIN`, `Brin`, `FullText`, `Spatial`, `Columnstore`, `Xml` — normalize across providers
- `FillFactor` (nullable int)
- `IsDisabled`

### 11. `Trigger`
- Name
- Timing: `Before`, `After`, `InsteadOf`
- Events: flags enum `Insert`, `Update`, `Delete`
- Body SQL (nullable)
- `IsDisabled`

### 12. `Sequence`
- Schema-qualified name
- Data type (`DbTypeCode`)
- Start value, increment, min value, max value, `IsCycling`
- Cache size (nullable)

### 13. `StoredProcedure`
- Schema-qualified name
- Ordered list of `Parameter` objects
- Body SQL (nullable)
- Description / comment

### 14. `ScalarFunction`
- Schema-qualified name
- Return type as a `TypeMapping` instance
- Ordered list of `Parameter` objects
- `IsDeterministic`
- Body SQL (nullable)

### 15. `TableValuedFunction`
- Schema-qualified name
- Ordered list of `Parameter` objects
- Return columns (list of `Column`-like descriptors)
- Body SQL (nullable)

### 17. `Parameter`
- Name, ordinal
- **`TypeMapping`** property (same shared type used by `Column`)
- Direction: `Input`, `Output`, `InputOutput`, `ReturnValue`
- Default value expression (nullable)

### 17. `UserDefinedType`
- Schema-qualified name
- Kind: `Alias` (type alias), `TableType` (table-valued parameter type), `Composite` (PostgreSQL), `Enum` (PostgreSQL), `Domain`
- Underlying type info (for aliases)
- Columns (for table types)
- Enum labels (for PostgreSQL enums)

---

## Supporting / Shared Types

### `TypeMapping`
A shared value type (record or record struct) that encapsulates the full type description for a `Column` or `Parameter`. Centralizes type information instead of scattering individual properties. Must include:
- `DbTypeCode` — the normalized enum value
- `NativeTypeName` (`string`) — the raw provider type name for round-tripping (e.g., `"nvarchar(256)"`, `"jsonb"`)
- `SystemType` (`Type`) — the .NET CLR type (e.g., `typeof(string)`, `typeof(int)`, `typeof(byte[])`). For nullable columns with value-type mappings, this should be the `Nullable<T>` variant (e.g., `typeof(int?)` not `typeof(int)`). Serialized as the type's `AssemblyQualifiedName` string via a custom `JsonConverter<Type>`, and resolved back via `Type.GetType()` on deserialization.
- `MaxLength` (`int?`)
- `Precision` (`int?`)
- `Scale` (`int?`)
- `IsUnicode` (`bool?`)
- `IsFixedLength` (`bool`)

Both `Column` and `Parameter` should reference a single `TypeMapping` property rather than duplicating these fields independently.

### `DbTypeCode` Enum
A normalized type code that covers the union of all common relational types. At minimum:

`Boolean`, `Byte`, `Int16`, `Int32`, `Int64`, `Decimal`, `Single`, `Double`,
`Currency`, `String`, `FixedString`, `Text`,
`Binary`, `FixedBinary`, `Blob`,
`Date`, `Time`, `DateTime`, `DateTimeOffset`, `Interval`,
`Guid`, `Xml`, `Json`, `JsonBinary`,
`Geometry`, `Geography`,
`RowVersion`, `Unknown`

### `ColumnReference`
- Reference to `Column`
- `SortDirection` enum: `Ascending`, `Descending`

### `IndexColumn` (extends `ColumnReference` concept)
- Same as `ColumnReference` plus `IsIncludedColumn` (covering index)

### `SchemaQualifiedName`
A value type (record struct) holding `Schema` + `Name` with proper equality, `ToString()` → `"schema.name"`, and implicit/explicit conversions.

### `ReferentialAction` Enum
`NoAction`, `Cascade`, `SetNull`, `SetDefault`, `Restrict`

### `DatabaseProvider` Record Struct
**Not an enum.** A value type (`readonly record struct`) with a single `string Name` property and case-insensitive equality. Provides static well-known instances:
- `DatabaseProvider.SqlServer` → `"SqlServer"`
- `DatabaseProvider.PostgreSql` → `"PostgreSql"`
- `DatabaseProvider.MySql` → `"MySql"`
- `DatabaseProvider.Oracle` → `"Oracle"`
- `DatabaseProvider.Sqlite` → `"Sqlite"`

But consumers can create arbitrary providers: `new DatabaseProvider("CockroachDb")`, `new DatabaseProvider("MariaDb")`, etc. This avoids a closed enum that would require model changes to support new databases. Serializes as a plain string via a custom `JsonConverter`.

---

## Architectural Requirements

1. **Builder pattern or factory.** Provide a `DatabaseModelBuilder` (or similar) that allows incremental construction — add tables, add columns, add indexes — then call `.Build()` to produce the frozen, immutable `DatabaseModel`. The builder should automatically wire up parent back-references and compute the `SchemaGroup` dictionary during build. Objects are added to the builder directly (not nested under a schema builder) since schemas are derived from the `SchemaQualifiedName` on each object.

2. **Visitor or enumeration support.** Provide an `IDatabaseModelVisitor` interface with `Visit(Table)`, `Visit(View)`, `Visit(Column)`, `Visit(Index)`, etc. so consumers can walk the entire model generically (useful for code generation, diffing, documentation). Consider a `Visit(RelationBase)` that dispatches to `Visit(Table)` or `Visit(View)` for consumers that don't need to distinguish.

3. **Annotations (single escape hatch for provider-specific data).** Every metadata object (`DatabaseModel`, `RelationBase`, `Column`, `Index`, `ForeignKey`, `Sequence`, `StoredProcedure`, `Parameter`, etc.) should implement a common `IAnnotatable` interface that exposes `IReadOnlyDictionary<string, object?> Annotations`. This is the **only** extensibility mechanism for provider-specific metadata — there is no separate "ExtendedProperties" concept. Use namespaced keys by convention (e.g., `"SqlServer:FileGroup"`, `"PostgreSql:Tablespace"`, `"SqlServer:IsMemoryOptimized"`). SQL Server `MS_Description` extended properties should be surfaced into the `Description` property on the core types, not stored as annotations.

4. **Equality and identity.** `SchemaQualifiedName` should be a proper value type with structural equality. Tables and columns should be identifiable by schema + name path.

5. **Full JSON round-trip serialization.** The model must serialize to and deserialize from JSON using `System.Text.Json` for caching, diffing, and transport.

   - **Use `[JsonPropertyName]` attributes** on every serialized property with `camelCase` names (e.g., `[JsonPropertyName("databaseName")]`). This decouples the C# naming convention from the serialized format and makes the JSON predictable for non-.NET consumers.
   - **Parent back-references** (e.g., `Column.Parent` → `RelationBase`, `Table.Database`, `Index.Parent`) must be marked `[JsonIgnore]` to avoid circular references during serialization.
   - **Post-deserialization rewiring.** Provide a method (e.g., `DatabaseModel.RewireParentReferences()` or handle it in a custom `JsonConverter<DatabaseModel>`) that walks the deserialized object graph and restores all parent back-references. This should be called automatically when deserializing via the provided converter so the consumer gets a fully navigable model without manual fixup. The builder's `.Build()` method should perform the same rewiring.
   - **Computed properties** like `SchemaGroup` dictionary should be excluded from serialization (`[JsonIgnore]`) and rebuilt during rewiring / post-deserialization.
   - **`ColumnReference` and `IndexColumn` serialization** — these hold references to `Column` objects. Serialize them by column name (string) and resolve the references back to actual `Column` instances during rewiring. Use a custom `JsonConverter` for these types.
   - **`ForeignKey` principal table reference** — serialize as a `SchemaQualifiedName` (string) and resolve back to the actual `Table` instance during rewiring.
   - **`DatabaseProvider`** — serialize as a plain string via a custom `JsonConverter`.
   - **`SystemType` (`Type`)** — the CLR `Type` property in `TypeMapping` must serialize as the type's full name string (e.g., `"System.String"`, `"System.Nullable`1[[System.Int32]]"`) via a custom `JsonConverter<Type>`. On deserialization, resolve back via `Type.GetType()`. Only BCL / runtime types will appear here, so assembly-qualified names are not necessary — short names like `"System.String"` suffice.
   - Provide a static convenience method: `DatabaseModel.FromJson(string json)` and an instance method `ToJson()` that use a pre-configured `JsonSerializerOptions` with all necessary converters registered.

---

## Output Format

- Produce **all** classes, records, enums, and interfaces in full — no summaries, no placeholders, no "// similar to above" shortcuts.
- Use XML doc comments on every public type and property.
- Group files logically (one type per file or closely related types together) and indicate the suggested file name as a comment at the top.
- Order the output: enums → interfaces → value types → core model types → JSON converters → builder → visitor → serialization helpers (`ToJson` / `FromJson`).

---

## What NOT To Include

- No provider-specific reader/scraper implementations (that's a separate concern).
- No code generation, migration, or diff logic.
- No EF Core dependencies or attributes.
- No constructor-injected services — this is a pure data model.
