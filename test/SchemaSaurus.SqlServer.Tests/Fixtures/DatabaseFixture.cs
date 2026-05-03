using FluentMigrator.Runner;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using SchemaSaurus.Migrator.Providers;

using Testcontainers.MsSql;

using XUnit.Hosting.Logging;

namespace SchemaSaurus.SqlServer.Tests.Fixtures;

public class DatabaseFixture : TestApplicationFixture, IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-latest")
        .WithPassword("Bn87bBYhLjYRj%9zRgUc")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        builder.Logging.AddMemoryLogger();

        var services = builder.Services;

        // change database from container default
        string containerConnection = _msSqlContainer.GetConnectionString();
        var connectionBuilder = new SqlConnectionStringBuilder(containerConnection)
        {
            InitialCatalog = "SchemaSaurus"
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
                .AddSqlServer()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(SqlServerDefault).Assembly, typeof(DatabaseFixture).Assembly)
                .For.All()
            );

        services
            .TryAddSingleton<IProviderDefault, SqlServerDefault>();

        services
            .AddHostedService<DatabaseInitializer>();
    }
}
