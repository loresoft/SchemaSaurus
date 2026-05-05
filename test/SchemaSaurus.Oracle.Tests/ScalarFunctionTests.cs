using System.Data;

using SchemaSaurus.Oracle.Tests.Fixtures;

using ParameterDirection = SchemaSaurus.Metadata.ParameterDirection;

namespace SchemaSaurus.Oracle.Tests;

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
    public async Task WhenReadingFormatAddressThenSchemaIsDefaultSchema()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.SchemaQualifiedName.Schema.Should().Be(model.DefaultSchemaName);
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
        func.Parameters.Should().Contain(p => p.Name == "AddressLine1");
        func.Parameters.Should().Contain(p => p.Name == "City");
        func.Parameters.Should().Contain(p => p.Name == "StateProvince");
        func.Parameters.Should().Contain(p => p.Name == "PostalCode");
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
    public async Task WhenReadingFormatAddressThenParametersAreInput()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.Parameters.Should().AllSatisfy(p => p.Direction.Should().Be(ParameterDirection.Input));
    }

    [Fact]
    public async Task WhenReadingFormatAddressThenParameterOrdinalsAreSequential()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.SchemaQualifiedName.Name == "FormatAddress");

        func.Parameters.Should().AllSatisfy(p => p.Ordinal.Should().BeGreaterThan(0));
        func.Parameters.Select(p => p.Ordinal).Should().BeInAscendingOrder();
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
