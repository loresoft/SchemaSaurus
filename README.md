# SchemaSaurus

SchemaSaurus is a .NET library for capturing a complete, immutable snapshot of a relational database's structural metadata — tables, views, indexes, foreign keys, stored procedures, functions, sequences, and user-defined types — into a single, JSON-serializable object graph.

It is designed for tools that need a stable, provider-agnostic view of a database schema: code generators, ORM tooling, migration diffing, documentation generation, and schema visualization.

[![Build status](https://github.com/loresoft/SchemaSaurus/actions/workflows/dotnet.yml/badge.svg)](https://github.com/loresoft/SchemaSaurus/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Coverage Status](https://coveralls.io/repos/github/loresoft/SchemaSaurus/badge.svg?branch=main)](https://coveralls.io/github/loresoft/SchemaSaurus?branch=main)

| Package                                                                            | Version                                                                                                                                           |
| :--------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------ |
| [SchemaSaurus.Metadata](https://www.nuget.org/packages/SchemaSaurus.Metadata/)     | [![SchemaSaurus.Metadata](https://img.shields.io/nuget/v/SchemaSaurus.Metadata.svg)](https://www.nuget.org/packages/SchemaSaurus.Metadata/)       |
| [SchemaSaurus.SqlServer](https://www.nuget.org/packages/SchemaSaurus.SqlServer/)   | [![SchemaSaurus.SqlServer](https://img.shields.io/nuget/v/SchemaSaurus.SqlServer.svg)](https://www.nuget.org/packages/SchemaSaurus.SqlServer/)    |
| [SchemaSaurus.PostgreSql](https://www.nuget.org/packages/SchemaSaurus.PostgreSql/) | [![SchemaSaurus.PostgreSql](https://img.shields.io/nuget/v/SchemaSaurus.PostgreSql.svg)](https://www.nuget.org/packages/SchemaSaurus.PostgreSql/) |
| [SchemaSaurus.MySql](https://www.nuget.org/packages/SchemaSaurus.MySql/)           | [![SchemaSaurus.MySql](https://img.shields.io/nuget/v/SchemaSaurus.MySql.svg)](https://www.nuget.org/packages/SchemaSaurus.MySql/)                |
| [SchemaSaurus.Oracle](https://www.nuget.org/packages/SchemaSaurus.Oracle/)         | [![SchemaSaurus.Oracle](https://img.shields.io/nuget/v/SchemaSaurus.Oracle.svg)](https://www.nuget.org/packages/SchemaSaurus.Oracle/)             |
| [SchemaSaurus.Sqlite](https://www.nuget.org/packages/SchemaSaurus.Sqlite/)         | [![SchemaSaurus.Sqlite](https://img.shields.io/nuget/v/SchemaSaurus.Sqlite.svg)](https://www.nuget.org/packages/SchemaSaurus.Sqlite/)             |


## Features

- **Immutable, JSON round-trippable model** rooted at `DatabaseModel` with structural equality.
- **Provider-agnostic abstraction** via `IDatabaseSchemaReader` — a single API across every supported engine.
- **Rich metadata coverage** — tables, columns, primary keys, unique constraints, check constraints, indexes, foreign keys, views, stored procedures, scalar and table-valued functions, sequences, user-defined types, and triggers.
- **Filtering** by schema, table, and object kind through `SchemaReaderOptions`.
- **Visitor pattern** (`DatabaseVisitor`) for walking the model in code generators and analyzers.
- **Annotations** on every metadata element for engine-specific extensions (collation, identity, extended properties, etc.).
- **Multi-target** — `netstandard2.0`, `net462`, `net8.0`, `net9.0`, and `net10.0`.

## Supported Providers

| Package                   | Minimum Supported Server                                                        |
| ------------------------- | ------------------------------------------------------------------------------- |
| `SchemaSaurus.SqlServer`  | SQL Server 2016 (13.x) or later, Azure SQL Database, Azure SQL Managed Instance |
| `SchemaSaurus.PostgreSql` | PostgreSQL 12 or later                                                          |
| `SchemaSaurus.MySql`      | MySQL 5.7 or later, MariaDB 10.2 or later                                       |
| `SchemaSaurus.Oracle`     | Oracle Database 12c (12.1) or later                                             |
| `SchemaSaurus.Sqlite`     | SQLite 3.31.0 or later                                                          |

All providers depend on the shared `SchemaSaurus.Metadata` package, which defines the model and abstractions.

## Installation

Install the metadata package plus the provider for your target engine:

```powershell
dotnet add package SchemaSaurus.Metadata
dotnet add package SchemaSaurus.SqlServer
```

## Quick Start

```csharp
using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.SqlServer;

IDatabaseSchemaReader reader = new SqlServerSchemaReader();

DatabaseModel model = await reader.ReadAsync("Server=.;Database=AdventureWorks;Integrated Security=true;TrustServerCertificate=true");

Console.WriteLine($"{model.DatabaseName} ({model.Provider} {model.ServerVersion})");
Console.WriteLine($"Tables: {model.Tables.Count}, Views: {model.Views.Count}");
```

### Filtering

```csharp
var options = new SchemaReaderOptions
{
    Schemas = ["dbo", "Sales"],
    IncludeStoredProcedures = false,
    IncludeScalarFunctions = false,
};

var model = await reader.ReadAsync(connectionString, options);
```

### JSON Serialization

The model uses source-generated `System.Text.Json` serialization via `MetadataJsonContext` for AOT-friendly, allocation-light round-tripping.

```csharp
using System.Text.Json;
using SchemaSaurus.Metadata;

string json = JsonSerializer.Serialize(model, MetadataJsonContext.Default.DatabaseModel);
DatabaseModel restored = JsonSerializer.Deserialize(json, MetadataJsonContext.Default.DatabaseModel)!;
```

### Visitor

Walk the model to drive code generation or analysis:

```csharp
public sealed class TableLogger : DatabaseVisitor
{
    protected override void VisitTable(Table table)
    {
        Console.WriteLine($"{table.Name} ({table.Columns.Count} columns)");
        base.VisitTable(table);
    }
}

new TableLogger().VisitDatabase(model);
```

## Model Overview

`DatabaseModel` is a flat container — every object carries its own `SchemaQualifiedName`, so consumers can group or filter by schema without traversing a hierarchy.

- `Tables` → `Columns`, `PrimaryKey`, `UniqueConstraints`, `CheckConstraints`, `Indexes`, `ForeignKeys`, `Triggers`
- `Views` → `Columns`, `Definition`
- `StoredProcedures` / `ScalarFunctions` / `TableValuedFunctions` → `Parameters`, `ReturnColumns`, `Definition`
- `Sequences`, `UserDefinedTypes`

Every metadata element implements `IAnnotatable` for engine-specific extension data.

## License

[MIT](LICENSE) © LoreSoft

