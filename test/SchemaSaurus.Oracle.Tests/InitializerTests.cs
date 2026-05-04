using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

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
