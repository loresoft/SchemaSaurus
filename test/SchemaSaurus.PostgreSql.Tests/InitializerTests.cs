using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class InitializerTests(DatabaseFixture databaseFixture)
    : DatabaseTestBase(databaseFixture)
{
    [Fact]
    public void GetRequiredService_IConfiguration()
    {
        var configuration = Services.GetRequiredService<IConfiguration>();
        configuration.Should().NotBeNull();
    }
}
