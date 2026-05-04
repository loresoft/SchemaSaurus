using FluentMigrator.Runner;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Oracle.ManagedDataAccess.Client;

using SchemaSaurus.Migrator.Providers;

using Testcontainers.Oracle;

using XUnit.Hosting.Logging;

namespace SchemaSaurus.Oracle.Tests.Fixtures;

public class DatabaseFixture : TestApplicationFixture, IAsyncLifetime
{
    private readonly OracleContainer _oracleContainer = new OracleBuilder("gvenzl/oracle-xe:21.3.0-slim-faststart")
        .WithDatabase("SchemaSaurus")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _oracleContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _oracleContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        builder.Logging.AddMemoryLogger();

        var services = builder.Services;

        // change database from container default
        string containerConnection = _oracleContainer.GetConnectionString();

        var configurationData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:SchemaSaurus"] = containerConnection,
            ["ConnectionStrings:ContainerConnection"] = containerConnection
        };
        builder.Configuration.AddInMemoryCollection(configurationData);

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddOracleManaged()
                .WithGlobalConnectionString(containerConnection)
                .ScanIn(typeof(OracleDefault).Assembly, typeof(DatabaseFixture).Assembly)
                .For.All()
            );

        services
            .TryAddSingleton<IProviderDefault, OracleDefault>();

        services
            .AddHostedService<DatabaseInitializer>();
    }
}
