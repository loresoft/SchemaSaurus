using System.Data;

using SchemaSaurus.Metadata;
using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

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
    public async Task WhenReadingUserDefinedTypesThenIdentifierObjectExists()
    {
        var model = await GetDatabaseModelAsync();

        model.UserDefinedTypes.Should().Contain(u => u.SchemaQualifiedName.Name == "IdentifierObject");
    }

    [Fact]
    public async Task WhenReadingIdentifierObjectTypeThenSchemaIsDefaultSchema()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "IdentifierObject");

        udt.SchemaQualifiedName.Schema.Should().Be(model.DefaultSchemaName);
    }

    [Fact]
    public async Task WhenReadingIdentifierObjectTypeThenKindIsComposite()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "IdentifierObject");

        udt.Kind.Should().Be(UserDefinedTypeKind.Composite);
    }

    [Fact]
    public async Task WhenReadingIdentifierObjectTypeThenColumnsExist()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "IdentifierObject");

        udt.Columns.Should().NotBeNullOrEmpty();
        udt.Columns.Should().Contain(c => c.Name == "Id");
        udt.Columns.Should().Contain(c => c.Name == "Name");
    }

    [Fact]
    public async Task WhenReadingIdentifierObjectTypeThenIdColumnIsInt32()
    {
        var model = await GetDatabaseModelAsync();
        var udt = model.UserDefinedTypes.First(u => u.SchemaQualifiedName.Name == "IdentifierObject");

        var idColumn = udt.Columns!.First(c => c.Name == "Id");
        idColumn.DbType.Should().Be(DbType.Int32);
        idColumn.SystemType.Should().Be(typeof(int));
    }

    [Fact]
    public async Task WhenReadingUserDefinedTypesThenSystemSchemasAreExcluded()
    {
        var model = await GetDatabaseModelAsync();

        var systemSchemas = new[]
        {
            "SYS",
            "SYSTEM",
            "OUTLN",
            "DBSNMP",
            "XDB",
            "CTXSYS",
            "MDSYS",
            "DVSYS",
            "GSMADMIN_INTERNAL",
            "LBACSYS",
            "OJVMSYS",
            "ORDDATA",
            "ORDSYS",
            "WMSYS",
        };

        model.UserDefinedTypes.Should().NotContain(u => systemSchemas.Contains(u.SchemaQualifiedName.Schema));
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
