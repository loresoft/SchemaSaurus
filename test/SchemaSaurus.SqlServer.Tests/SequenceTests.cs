using System.Data;

using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

public class SequenceTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingSequencesThenSequencesAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        model.Sequences.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingSequencesThenOrderNumberSequenceExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Sequences.Should().Contain(s => s.SchemaQualifiedName.Name == "OrderNumberSequence");
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenSchemaIsDbo()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.SchemaQualifiedName.Name == "OrderNumberSequence");

        sequence.SchemaQualifiedName.Schema.Should().Be("dbo");
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenStartValueIs1000()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.SchemaQualifiedName.Name == "OrderNumberSequence");

        sequence.StartValue.Should().Be(1000);
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenIncrementIs1()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.SchemaQualifiedName.Name == "OrderNumberSequence");

        sequence.Increment.Should().Be(1);
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenTypeIsMapped()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.SchemaQualifiedName.Name == "OrderNumberSequence");

        sequence.DbType.Should().Be(DbType.Int64);
        sequence.SystemType.Should().Be(typeof(long));
    }

    [Fact]
    public async Task WhenExcludingSequencesThenNoSequencesReturned()
    {
        var options = new Metadata.Provider.SchemaReaderOptions
        {
            IncludeSequences = false
        };

        var model = await GetDatabaseModelAsync(options);

        model.Sequences.Should().BeEmpty();
    }
}
