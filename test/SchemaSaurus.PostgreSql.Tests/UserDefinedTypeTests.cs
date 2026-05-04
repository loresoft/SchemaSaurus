using System.Data;

using SchemaSaurus.Metadata;
using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class UserDefinedTypeTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingUserDefinedTypesThenTypesAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        model.UserDefinedTypes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingUserDefinedTypesThenTaskStateExists()
    {
        var model = await GetDatabaseModelAsync();

        model.UserDefinedTypes.Should().Contain(u => u.SchemaQualifiedName.Name == "TaskState");
    }

    [Fact]
    public async Task WhenReadingTaskStateTypeThenSchemaIsPublic()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "TaskState");

        udt.SchemaQualifiedName.Schema.Should().Be("public");
    }

    [Fact]
    public async Task WhenReadingTaskStateTypeThenKindIsEnum()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "TaskState");

        udt.Kind.Should().Be(UserDefinedTypeKind.Enum);
    }

    [Fact]
    public async Task WhenReadingTaskStateTypeThenLabelsExist()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "TaskState");

        udt.EnumLabels.Should().Contain(["New", "Active", "Closed"]);
    }

    [Fact]
    public async Task WhenReadingEmailAddressDomainThenBaseTypeIsString()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "EmailAddress");

        udt.Kind.Should().Be(UserDefinedTypeKind.Domain);
        udt.DbType.Should().Be(DbType.String);
        udt.SystemType.Should().Be(typeof(string));
    }

    [Fact]
    public async Task WhenReadingIdentifierPairTypeThenKindIsComposite()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "IdentifierPair");

        udt.Kind.Should().Be(UserDefinedTypeKind.Composite);
    }

    [Fact]
    public async Task WhenExcludingUserDefinedTypesThenNoTypesReturned()
    {
        var options = new Metadata.Provider.SchemaReaderOptions
        {
            IncludeUserDefinedTypes = false
        };

        var model = await GetDatabaseModelAsync(options);

        model.UserDefinedTypes.Should().BeEmpty();
    }
}
