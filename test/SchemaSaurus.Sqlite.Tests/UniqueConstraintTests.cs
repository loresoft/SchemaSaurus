using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class UniqueConstraintTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingUniqueConstraintTableThenUniqueConstraintsAreDiscovered()
    {
        var model = await GetUniqueConstraintTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "UniqueConstraint");

        table.UniqueConstraints.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenReadingSingleColumnUniqueConstraintThenColumnIsPopulated()
    {
        var model = await GetUniqueConstraintTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "UniqueConstraint");
        var uniqueConstraint = table.UniqueConstraints.Single(c => c.Columns.Count == 1);

        uniqueConstraint.Columns.Should().ContainSingle()
            .Which.ColumnName.Should().Be("Code");
    }

    [Fact]
    public async Task WhenReadingCompositeUniqueConstraintThenColumnsArePopulatedInOrdinalOrder()
    {
        var model = await GetUniqueConstraintTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "UniqueConstraint");
        var uniqueConstraint = table.UniqueConstraints.Single(c => c.Columns.Count == 2);

        uniqueConstraint.Columns.Should().HaveCount(2);
        uniqueConstraint.Columns[0].ColumnName.Should().Be("TenantId");
        uniqueConstraint.Columns[1].ColumnName.Should().Be("ExternalId");
    }

    [Fact]
    public async Task WhenReadingUniqueConstraintThenGeneratedSqliteNamesAreNormalized()
    {
        var model = await GetUniqueConstraintTableModelAsync();
        var table = model.Tables.Single(t => t.QualifiedName.Name == "UniqueConstraint");

        table.UniqueConstraints.Select(c => c.Name).Should().BeEquivalentTo(
            "uq_UniqueConstraint_1",
            "uq_UniqueConstraint_2");
        table.UniqueConstraints.Should().OnlyContain(c => !c.Name.StartsWith("sqlite_", StringComparison.Ordinal));
    }

    private Task<DatabaseModel> GetUniqueConstraintTableModelAsync()
    {
        var options = new SchemaReaderOptions
        {
            Tables = ["UniqueConstraint"],
            IncludeViews = false,
            IncludeStoredProcedures = false,
            IncludeScalarFunctions = false,
            IncludeTableValuedFunctions = false,
            IncludeSequences = false,
            IncludeUserDefinedTypes = false,
        };

        return GetDatabaseModelAsync(options);
    }
}
