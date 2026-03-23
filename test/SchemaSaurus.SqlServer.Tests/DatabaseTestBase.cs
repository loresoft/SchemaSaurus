using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

[Collection(DatabaseCollection.CollectionName)]
public abstract class DatabaseTestBase(DatabaseFixture databaseFixture)
    : TestHostBase<DatabaseFixture>(databaseFixture)
{
}
