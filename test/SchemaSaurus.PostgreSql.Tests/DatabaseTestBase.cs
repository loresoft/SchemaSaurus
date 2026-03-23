using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

[Collection(DatabaseCollection.CollectionName)]
public abstract class DatabaseTestBase(DatabaseFixture databaseFixture)
    : TestHostBase<DatabaseFixture>(databaseFixture)
{
}
