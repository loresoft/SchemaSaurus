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

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
