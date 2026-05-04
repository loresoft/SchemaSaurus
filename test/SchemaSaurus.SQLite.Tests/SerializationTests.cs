using SchemaSaurus.Metadata;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class SerializationTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenModelSavesToJson()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().NotBeEmpty();

        var json = model.ToJson();
        File.WriteAllText("SerializationTests_Snapshot.json", json);
    }
}
