using FluentMigrator.Runner;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using MySqlConnector;

using SchemaSaurus.Migrator.Providers;

using Testcontainers.MySql;

using XUnit.Hosting.Logging;

namespace SchemaSaurus.MySql.Tests.Fixtures;

public class DatabaseFixture : TestApplicationFixture, IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer = new MySqlBuilder("mysql:8").Build();

    public async ValueTask InitializeAsync()
    {
        await _mySqlContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _mySqlContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        builder.Logging.AddMemoryLogger();

        var services = builder.Services;

        // change database from container default
        string containerConnection = _mySqlContainer.GetConnectionString();
        var connectionBuilder = new MySqlConnectionStringBuilder(containerConnection)
        {
            Database = "SchemaSaurus"
        };
        var connectionString = connectionBuilder.ToString();

        // override connection string to use docker container
        var configurationData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:SchemaSaurus"] = connectionString,
            ["ConnectionStrings:ContainerConnection"] = containerConnection
        };
        builder.Configuration.AddInMemoryCollection(configurationData);

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddMySql8()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(MySqlDefault).Assembly, typeof(DatabaseFixture).Assembly)
                .For.All()
            );

        services
            .TryAddSingleton<IProviderDefault, MySqlDefault>();

        services
            .AddHostedService<DatabaseInitializer>();
    }
}
