using FluentMigrator.Runner;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using SchemaSaurus.Migrator.Providers;

using Testcontainers.PostgreSql;

using XUnit.Hosting.Logging;

namespace SchemaSaurus.PostgreSql.Tests.Fixtures;

public class DatabaseFixture : TestApplicationFixture, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("SchemaSaurus")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        builder.Logging.AddMemoryLogger();

        var services = builder.Services;

        // change database from container default
        string containerConnection = _postgreSqlContainer.GetConnectionString();
        var connectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(containerConnection)
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
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(PostgreSqlDefault).Assembly, typeof(DatabaseFixture).Assembly)
                .For.All()
            );

        services
            .TryAddSingleton<IProviderDefault, PostgreSqlDefault>();

        services
            .AddHostedService<DatabaseInitializer>();
    }
}
