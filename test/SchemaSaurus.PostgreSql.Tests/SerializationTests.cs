using SchemaSaurus.Metadata;
using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class SerializationTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenModelSavesToJson()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().NotBeEmpty();

        var json = model.ToJson();
        File.WriteAllText("PostgreSqlSerializationTests_Snapshot.json", json);
    }
}
