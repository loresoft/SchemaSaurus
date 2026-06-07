using System.Data;

using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.Sqlite.Tests.Fixtures;

namespace SchemaSaurus.Sqlite.Tests;

public class TableSchemaTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    [Fact]
    public async Task WhenReadingSchemaTablesThenTablesAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenUserTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.QualifiedName.Name == "User");
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenStatusTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.QualifiedName.Name == "Status");
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenTaskTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.QualifiedName.Name == "Task");
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenQuotedTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.QualifiedName.Name == "Computed Column");
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenTablesHaveNoSchema()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();
        userTable.QualifiedName.Schema.Should().BeNull();
    }

    [Fact]
    public async Task WhenReadingUserTableThenColumnsAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        userTable.Columns.Should().NotBeEmpty();
        userTable.Columns.Should().Contain(c => c.Name == "Id");
        userTable.Columns.Should().Contain(c => c.Name == "UserName");
        userTable.Columns.Should().Contain(c => c.Name == "EmailAddress");
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnIsIdentity()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var idColumn = userTable.Columns.FirstOrDefault(c => c.Name == "Id");
        idColumn.Should().NotBeNull();
        idColumn.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnUsesIntegerAffinity()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var idColumn = userTable.Columns.FirstOrDefault(c => c.Name == "Id");
        idColumn.Should().NotBeNull();
        idColumn.DbType.Should().Be(DbType.Int64);
        idColumn.SystemType.Should().Be(typeof(long));
        idColumn.NativeTypeName.Should().Be("INTEGER");
    }

    [Fact]
    public async Task WhenReadingUserTableThenUserNameIsString()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var userNameColumn = userTable.Columns.FirstOrDefault(c => c.Name == "UserName");
        userNameColumn.Should().NotBeNull();
        userNameColumn.DbType.Should().Be(DbType.String);
        userNameColumn.SystemType.Should().Be(typeof(string));
        userNameColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingUserTableThenNullableColumnIsMarkedNullable()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "User");
        userTable.Should().NotBeNull();

        var firstNameColumn = userTable.Columns.FirstOrDefault(c => c.Name == "FirstName");
        firstNameColumn.Should().NotBeNull();
        firstNameColumn.IsNullable.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenDefaultValueSqlIsPopulated()
    {
        var model = await GetDatabaseModelAsync();

        var statusTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Status");
        statusTable.Should().NotBeNull();

        var displayOrderColumn = statusTable.Columns.FirstOrDefault(c => c.Name == "DisplayOrder");
        displayOrderColumn.Should().NotBeNull();
        displayOrderColumn.DefaultValueSql.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenIsActiveHasDefaultValue()
    {
        var model = await GetDatabaseModelAsync();

        var statusTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Status");
        statusTable.Should().NotBeNull();

        var isActiveColumn = statusTable.Columns.FirstOrDefault(c => c.Name == "IsActive");
        isActiveColumn.Should().NotBeNull();
        isActiveColumn.DefaultValueSql.Should().NotBeNullOrWhiteSpace();
        isActiveColumn.DbType.Should().Be(DbType.Int64);
    }

    [Fact]
    public async Task WhenReadingTaskTableThenGuidPrimaryKeyExists()
    {
        var model = await GetDatabaseModelAsync();

        var taskTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Task");
        taskTable.Should().NotBeNull();

        var idColumn = taskTable.Columns.FirstOrDefault(c => c.Name == "Id");
        idColumn.Should().NotBeNull();
        idColumn.DbType.Should().Be(DbType.Guid);
        idColumn.SystemType.Should().Be(typeof(Guid));
        idColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingDataTypeTableThenVariousTypesAreMapped()
    {
        var model = await GetDatabaseModelAsync();

        var dataTypeTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "DataType");
        dataTypeTable.Should().NotBeNull();

        var booleanColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "Boolean");
        var shortColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "Short");
        var longColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "Long");
        var floatColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "Float");
        var doubleColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "Double");
        var decimalColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "Decimal");
        var dateTimeColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "DateTime");
        var dateTimeOffsetColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "DateTimeOffset");
        var guidColumn = dataTypeTable.Columns.FirstOrDefault(c => c.Name == "Guid");

        booleanColumn.Should().NotBeNull();
        shortColumn.Should().NotBeNull();
        longColumn.Should().NotBeNull();
        floatColumn.Should().NotBeNull();
        doubleColumn.Should().NotBeNull();
        decimalColumn.Should().NotBeNull();
        dateTimeColumn.Should().NotBeNull();
        dateTimeOffsetColumn.Should().NotBeNull();
        guidColumn.Should().NotBeNull();

        booleanColumn.DbType.Should().Be(DbType.Int64);
        shortColumn.DbType.Should().Be(DbType.Int64);
        longColumn.DbType.Should().Be(DbType.Int64);
        floatColumn.DbType.Should().Be(DbType.Decimal);
        doubleColumn.DbType.Should().Be(DbType.Decimal);
        decimalColumn.DbType.Should().Be(DbType.Decimal);
        dateTimeColumn.DbType.Should().Be(DbType.DateTime);
        dateTimeOffsetColumn.DbType.Should().Be(DbType.DateTime);
        guidColumn.DbType.Should().Be(DbType.Guid);
    }

    [Fact]
    public async Task WhenFilteringByTableNameThenOnlyMatchingTablesReturned()
    {
        var options = new SchemaReaderOptions
        {
            Tables = ["Status"]
        };

        var model = await GetDatabaseModelAsync(options);

        model.Tables.Should().HaveCount(1);
        model.Tables[0].QualifiedName.Name.Should().Be("Status");
    }

    [Fact]
    public async Task WhenReadingColumnsOrdinalPositionsArePopulated()
    {
        var model = await GetDatabaseModelAsync();

        var statusTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Status");
        statusTable.Should().NotBeNull();

        statusTable.Columns.Should().AllSatisfy(c => c.OrdinalPosition.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task WhenReadingGeneratedColumnsThenVirtualColumnIsComputed()
    {
        var model = await GetDatabaseModelAsync();

        var computedTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Computed Column");
        computedTable.Should().NotBeNull();

        var fullNameColumn = computedTable.Columns.FirstOrDefault(c => c.Name == "Full Name");
        fullNameColumn.Should().NotBeNull();
        fullNameColumn.IsComputed.Should().BeTrue();
        fullNameColumn.IsStored.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingGeneratedColumnsThenStoredColumnIsComputed()
    {
        var model = await GetDatabaseModelAsync();

        var computedTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "Computed Column");
        computedTable.Should().NotBeNull();

        var searchNameColumn = computedTable.Columns.FirstOrDefault(c => c.Name == "Search Name");
        searchNameColumn.Should().NotBeNull();
        searchNameColumn.IsComputed.Should().BeTrue();
        searchNameColumn.IsStored.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingSpatialDataTableThenSpatialColumnsAreMapped()
    {
        var model = await GetDatabaseModelAsync();

        var spatialTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "SpatialData");
        spatialTable.Should().NotBeNull();

        var geometryColumn = spatialTable.Columns.FirstOrDefault(c => c.Name == "GeometryValue");
        geometryColumn.Should().NotBeNull();
        geometryColumn.DbType.Should().Be(DbType.Object);
        geometryColumn.SystemType.Should().Be(typeof(byte[]));
        geometryColumn.NativeTypeName.Should().Be("GEOMETRY");

        var pointColumn = spatialTable.Columns.FirstOrDefault(c => c.Name == "PointValue");
        pointColumn.Should().NotBeNull();
        pointColumn.DbType.Should().Be(DbType.Object);
        pointColumn.SystemType.Should().Be(typeof(byte[]));
        pointColumn.NativeTypeName.Should().Be("POINT");
    }
}
