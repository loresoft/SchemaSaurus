using SchemaSaurus.Metadata;
using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

public class SerializationTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenModelSavesToJson()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().NotBeEmpty();

        var json = model.ToJson();

        File.WriteAllText("..\\..\\..\\..\\TestResults\\Oracle_Snapshot.json", json);
    }
}
