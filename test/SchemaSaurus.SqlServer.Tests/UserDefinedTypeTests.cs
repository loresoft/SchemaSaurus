using System.Data;

using SchemaSaurus.Metadata;
using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

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
    public async Task WhenReadingUserDefinedTypesThenIdentifierTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.UserDefinedTypes.Should().Contain(u => u.QualifiedName.Name == "IdentifierTable");
    }

    [Fact]
    public async Task WhenReadingIdentifierTableTypeThenSchemaIsDbo()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.QualifiedName.Name == "IdentifierTable");

        udt.QualifiedName.Schema.Should().Be("dbo");
    }

    [Fact]
    public async Task WhenReadingIdentifierTableTypeThenKindIsTableType()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.QualifiedName.Name == "IdentifierTable");

        udt.Kind.Should().Be(UserDefinedTypeKind.TableType);
    }

    [Fact]
    public async Task WhenReadingIdentifierTableTypeThenColumnsExist()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.QualifiedName.Name == "IdentifierTable");

        udt.Columns.Should().NotBeNullOrEmpty();
        udt.Columns.Should().Contain(c => c.Name == "Id");
    }

    [Fact]
    public async Task WhenReadingIdentifierTableTypeThenIdColumnIsInt32()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.QualifiedName.Name == "IdentifierTable");

        var idColumn = udt.Columns!.First(c => c.Name == "Id");
        idColumn.DbType.Should().Be(DbType.Int32);
        idColumn.SystemType.Should().Be(typeof(int));
        idColumn.IsNullable.Should().BeFalse();
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
