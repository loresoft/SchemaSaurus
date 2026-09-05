using SchemaSaurus.Metadata;
using SchemaSaurus.PostgreSql.Tests.Fixtures;

namespace SchemaSaurus.PostgreSql.Tests;

public class UnnamedParameterTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenFunctionHasUnnamedInputWithNamedOutputThenParametersArePositionallyNamed()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.QualifiedName.Name == "UnnamedInputWithOutput");

        func.Parameters.Should().HaveCount(2);

        var input = func.Parameters.Single(p => p.Ordinal == 1);
        input.Name.Should().Be("$1");
        input.Direction.Should().Be(ParameterDirection.Input);
        input.SystemType.Should().Be(typeof(int));

        var output = func.Parameters.Single(p => p.Ordinal == 2);
        output.Name.Should().Be("Result");
        output.Direction.Should().Be(ParameterDirection.Output);
    }

    [Fact]
    public async Task WhenTableValuedFunctionHasUnnamedInputThenParameterIsPositionallyNamed()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.TableValuedFunctions.First(f => f.QualifiedName.Name == "UnnamedInputReturnsTable");

        func.Parameters.Should().HaveCount(1);
        func.Parameters[0].Name.Should().Be("$1");
        func.Parameters[0].Ordinal.Should().Be(1);
    }

    [Fact]
    public async Task WhenFunctionHasNoNamedParametersThenParameterIsPositionallyNamed()
    {
        var model = await GetDatabaseModelAsync();
        var func = model.ScalarFunctions.First(f => f.QualifiedName.Name == "UnnamedInputScalar");

        func.Parameters.Should().HaveCount(1);
        func.Parameters[0].Name.Should().Be("$1");
    }
}
