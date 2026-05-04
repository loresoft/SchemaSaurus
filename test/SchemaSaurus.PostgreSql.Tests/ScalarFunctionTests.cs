using System.Data;

using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class ScalarFunctionTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingScalarFunctionsThenFunctionsAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        model.ScalarFunctions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingScalarFunctionsThenFormatAddressExists()
    {
        var model = await GetDatabaseModelAsync();

        model.ScalarFunctions.Should().Contain(f => f.SchemaQualifiedName.Name == "FormatAddress");
    }

    [Fact]
    public async Task WhenReadingFormatAddressThenSchemaIsPublic()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.SchemaQualifiedName.Schema.Should().Be("public");
    }

    [Fact]
    public async Task WhenReadingFormatAddressThenReturnTypeIsString()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.ReturnType.DbType.Should().Be(DbType.String);
        func.ReturnType.SystemType.Should().Be(typeof(string));
    }

    [Fact]
    public async Task WhenReadingFormatAddressThenDefinitionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.Definition.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenExcludingScalarFunctionsThenNoFunctionsReturned()
    {
        var options = new Metadata.Provider.SchemaReaderOptions
        {
            IncludeScalarFunctions = false
        };

        var model = await GetDatabaseModelAsync(options);

        model.ScalarFunctions.Should().BeEmpty();
    }
}
