using FluentMigrator.Runner;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using SchemaSaurus.Migrator.Providers;
using SchemaSaurus.Sqlite.Tests.Migrations;

using XUnit.Hosting.Logging;

namespace SchemaSaurus.Sqlite.Tests.Fixtures;

public class DatabaseFixture : TestApplicationFixture
{
    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        builder.Logging.AddMemoryLogger();

        var services = builder.Services;

        var connectionString = "Data Source=SchemaSaurus.db";
        var configurationData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:SchemaSaurus"] = connectionString,
        };
        builder.Configuration.AddInMemoryCollection(configurationData);

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(SqliteDefault).Assembly, typeof(CreateSpatialDataTable).Assembly)
                .For.All()
            );

        services
            .TryAddSingleton<IProviderDefault, SqliteDefault>();

        services
            .AddHostedService<DatabaseInitializer>();
    }
}
