using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

[Collection(DatabaseCollection.CollectionName)]
public abstract class DatabaseTestBase(DatabaseFixture databaseFixture)
    : TestHostBase<DatabaseFixture>(databaseFixture)
{
}
