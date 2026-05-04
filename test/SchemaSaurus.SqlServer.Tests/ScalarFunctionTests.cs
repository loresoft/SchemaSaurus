using System.Data;

using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

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
    public async Task WhenReadingFormatAddressThenSchemaIsDbo()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.SchemaQualifiedName.Schema.Should().Be("dbo");
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
    public async Task WhenReadingFormatAddressThenParametersExist()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.Parameters.Should().HaveCount(4);
        func.Parameters.Should().Contain(p => p.Name == "@AddressLine1");
        func.Parameters.Should().Contain(p => p.Name == "@City");
        func.Parameters.Should().Contain(p => p.Name == "@StateProvince");
        func.Parameters.Should().Contain(p => p.Name == "@PostalCode");
    }

    [Fact]
    public async Task WhenReadingFormatAddressThenParameterTypesAreCorrect()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.Parameters.Should().AllSatisfy(p =>
        {
            p.DbType.Should().Be(DbType.String);
            p.SystemType.Should().Be(typeof(string));
        });
    }

    [Fact]
    public async Task WhenReadingFormatAddressThenDefinitionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.Definition.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingFormatAddressThenDescriptionIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.Description.Should().Be("Formats an address.");
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
