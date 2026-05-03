using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

public class ExtendedPropertiesTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingDatabaseThenExtendedPropertiesAreAnnotations()
    {
        var model = await GetDatabaseModelAsync();

        model.Annotations.Should().ContainKey("SchemaSaurus:Environment")
            .WhoseValue.Should().Be("IntegrationTests");
    }

    [Fact]
    public async Task WhenReadingTableThenMsDescriptionIsDescription()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

        userTable.Description.Should().Be("Application users.");
    }

    [Fact]
    public async Task WhenReadingTableThenCustomExtendedPropertiesAreAnnotations()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

        userTable.Annotations.Should().ContainKey("SchemaSaurus:AggregateRoot")
            .WhoseValue.Should().Be("True");
    }

    [Fact]
    public async Task WhenReadingColumnThenMsDescriptionIsDescription()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");
        var emailColumn = userTable.Columns.First(c => c.Name == "EmailAddress");

        emailColumn.Description.Should().Be("Primary email address for the user.");
    }

    [Fact]
    public async Task WhenReadingColumnThenCustomExtendedPropertiesAreAnnotations()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");
        var emailColumn = userTable.Columns.First(c => c.Name == "EmailAddress");

        emailColumn.Annotations.Should().ContainKey("SchemaSaurus:IsSensitive")
            .WhoseValue.Should().Be("True");
    }

    [Fact]
    public async Task WhenReadingIndexThenCustomExtendedPropertiesAreAnnotations()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");
        var emailIndex = userTable.Indexes.First(i => i.Name == "UX_User_EmailAddress");

        emailIndex.Annotations.Should().ContainKey("SchemaSaurus:Purpose")
            .WhoseValue.Should().Be("Enforce unique email addresses.");
    }
}
