using FluentMigrator.Runner;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SchemaSaurus.Sqlite.Tests.Fixtures;

public class DatabaseInitializer : IHostedService
{
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IMigrationRunner _migrationRunner;

    public DatabaseInitializer(
        ILogger<DatabaseInitializer> logger,
        IMigrationRunner migrationRunner)
    {
        _logger = logger;
        _migrationRunner = migrationRunner;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting database migration for {DatabaseType}...",
            _migrationRunner.Processor.DatabaseType);

        // Enable foreign key constraints
        _migrationRunner.Processor.Execute("PRAGMA foreign_keys = ON");

        // run migrations
        _migrationRunner.MigrateUp();

        CreateSqliteEdgeCaseObjects();

        return Task.CompletedTask;
    }

    private void CreateSqliteEdgeCaseObjects()
    {
        _migrationRunner.Processor.Execute("""
            CREATE TABLE IF NOT EXISTS "Computed Column" (
                "Id" INTEGER PRIMARY KEY,
                "First Name" TEXT NOT NULL,
                "Last Name" TEXT NOT NULL,
                "Full Name" TEXT GENERATED ALWAYS AS ("First Name" || ' ' || "Last Name") VIRTUAL,
                "Search Name" TEXT GENERATED ALWAYS AS (lower("First Name" || ' ' || "Last Name")) STORED
            )
            """);

        _migrationRunner.Processor.Execute("""
            CREATE INDEX IF NOT EXISTS "IX_Computed Column_Search Name_Active"
            ON "Computed Column" ("Search Name")
            WHERE "Last Name" <> ''
            """);

        _migrationRunner.Processor.Execute("""
            CREATE INDEX IF NOT EXISTS "IX_Computed Column_Lower_First Name"
            ON "Computed Column" (lower("First Name"))
            """);

        _migrationRunner.Processor.Execute("""
            CREATE VIEW IF NOT EXISTS "Active Users" AS
            SELECT "Id", "UserName", "EmailAddress"
            FROM "User"
            WHERE "IsDeleted" = 0
            """);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
