using SchemaSaurus.Metadata;
using SchemaSaurus.MySql.Tests.Fixtures;

namespace SchemaSaurus.MySql.Tests;

public class SerializationTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenModelSavesToJson()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().NotBeEmpty();

        var json = model.ToJson();

        File.WriteAllText("..\\..\\..\\..\\TestResults\\MySql_Snapshot.json", json);
    }
}
