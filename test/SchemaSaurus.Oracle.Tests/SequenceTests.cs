using System.Data;

using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

public class SequenceTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    private const string OrderNumberSequenceName = "ORDERNUMBERSEQUENCE";

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

        model.Sequences.Should().Contain(s => s.QualifiedName.Name == OrderNumberSequenceName);
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenSchemaIsDefaultSchema()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.QualifiedName.Name == OrderNumberSequenceName);

        sequence.QualifiedName.Schema.Should().Be(model.DefaultSchemaName);
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenStartValueIs1000()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.QualifiedName.Name == OrderNumberSequenceName);

        sequence.StartValue.Should().Be(1);
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenIncrementIs1()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.QualifiedName.Name == OrderNumberSequenceName);

        sequence.Increment.Should().Be(1);
    }

    [Fact]
    public async Task WhenReadingOrderNumberSequenceThenTypeIsMapped()
    {
        var model = await GetDatabaseModelAsync();
        var sequence = model.Sequences.First(s => s.QualifiedName.Name == OrderNumberSequenceName);

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
