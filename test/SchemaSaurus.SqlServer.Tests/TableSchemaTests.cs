using System.Data;

using SchemaSaurus.Metadata;
using SchemaSaurus.SqlServer.Tests.Fixtures;

namespace SchemaSaurus.SqlServer.Tests;

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
    public async Task WhenReadingSchemaTablesThenTablesHaveDboSchema()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");
        userTable.QualifiedName.Schema.Should().Be("dbo");
    }

    [Fact]
    public async Task WhenReadingUserTableThenColumnsAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        userTable.Columns.Should().NotBeEmpty();
        userTable.Columns.Should().Contain(c => c.Name == "Id");
        userTable.Columns.Should().Contain(c => c.Name == "UserName");
        userTable.Columns.Should().Contain(c => c.Name == "EmailAddress");
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnIsIdentity()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var idColumn = userTable.Columns.First(c => c.Name == "Id");
        idColumn.IsIdentity.Should().BeTrue();
        idColumn.IdentitySeed.Should().Be(1);
        idColumn.IdentityIncrement.Should().Be(1);
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnIsInt32()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var idColumn = userTable.Columns.First(c => c.Name == "Id");
        idColumn.DbType.Should().Be(DbType.Int32);
        idColumn.SystemType.Should().Be(typeof(int));

        idColumn
            .Annotations.Should().ContainKey(SqlServerAnnotations.SqlDbType)
            .WhoseValue.Should().Be("Int");
    }

    [Fact]
    public async Task WhenReadingUserTableThenUserNameIsAnsiString()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var userNameColumn = userTable.Columns.First(c => c.Name == "UserName");
        userNameColumn.DbType.Should().Be(DbType.AnsiString);
        userNameColumn.IsUnicode.Should().Be(false);
        userNameColumn.MaxLength.Should().Be(50);
        userNameColumn.IsNullable.Should().BeFalse();

        userNameColumn
            .Annotations.Should().ContainKey(SqlServerAnnotations.SqlDbType)
            .WhoseValue.Should().Be("VarChar");
    }

    [Fact]
    public async Task WhenReadingUserTableThenEmailAddressIsUnicodeString()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var emailColumn = userTable.Columns.First(c => c.Name == "EmailAddress");
        emailColumn.DbType.Should().Be(DbType.String);
        emailColumn.IsUnicode.Should().Be(true);
        emailColumn.MaxLength.Should().Be(256);
        emailColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingUserTableThenNullableColumnIsMarkedNullable()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == "User");

        var firstNameColumn = userTable.Columns.First(c => c.Name == "FirstName");
        firstNameColumn.IsNullable.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenRowVersionColumnExists()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == "Status");

        var rowVersionColumn = statusTable.Columns.First(c => c.Name == "RowVersion");
        rowVersionColumn.IsRowVersion.Should().BeTrue();
        rowVersionColumn.DbType.Should().Be(DbType.Binary);
    }

    [Fact]
    public async Task WhenReadingStatusTableThenDefaultValueSqlIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == "Status");

        var displayOrderColumn = statusTable.Columns.First(c => c.Name == "DisplayOrder");
        displayOrderColumn.DefaultValueSql.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenIsActiveHasDefaultValue()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == "Status");

        var isActiveColumn = statusTable.Columns.First(c => c.Name == "IsActive");
        isActiveColumn.DefaultValueSql.Should().NotBeNullOrWhiteSpace();
        isActiveColumn.DbType.Should().Be(DbType.Boolean);
    }

    [Fact]
    public async Task WhenReadingTaskTableThenGuidPrimaryKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == "Task");

        var idColumn = taskTable.Columns.First(c => c.Name == "Id");
        idColumn.DbType.Should().Be(DbType.Guid);
        idColumn.SystemType.Should().Be(typeof(Guid));
        idColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingDataTypeTableThenVariousTypesAreMapped()
    {
        var model = await GetDatabaseModelAsync();
        var dataTypeTable = model.Tables.First(t => t.QualifiedName.Name == "DataType");

        dataTypeTable.Columns.First(c => c.Name == "Boolean").DbType.Should().Be(DbType.Boolean);
        dataTypeTable.Columns.First(c => c.Name == "Short").DbType.Should().Be(DbType.Int16);
        dataTypeTable.Columns.First(c => c.Name == "Long").DbType.Should().Be(DbType.Int64);
        dataTypeTable.Columns.First(c => c.Name == "Float").DbType.Should().Be(DbType.Single);
        dataTypeTable.Columns.First(c => c.Name == "Double").DbType.Should().Be(DbType.Double);
        dataTypeTable.Columns.First(c => c.Name == "Decimal").DbType.Should().Be(DbType.Decimal);
        dataTypeTable.Columns.First(c => c.Name == "DateTime").DbType.Should().Be(DbType.DateTime);
        dataTypeTable.Columns.First(c => c.Name == "DateTimeOffset").DbType.Should().Be(DbType.DateTimeOffset);
        dataTypeTable.Columns.First(c => c.Name == "Guid").DbType.Should().Be(DbType.Guid);
    }

    [Fact]
    public async Task WhenReadingDataTypeTableThenDecimalHasPrecisionAndScale()
    {
        var model = await GetDatabaseModelAsync();
        var dataTypeTable = model.Tables.First(t => t.QualifiedName.Name == "DataType");

        var decimalColumn = dataTypeTable.Columns.First(c => c.Name == "Decimal");
        decimalColumn.Precision.Should().Be(19);
        decimalColumn.Scale.Should().Be(4);
    }

    [Fact]
    public async Task WhenFilteringByTableNameThenOnlyMatchingTablesReturned()
    {
        var options = new Metadata.Provider.SchemaReaderOptions
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
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == "Status");

        statusTable.Columns.Should().AllSatisfy(c => c.OrdinalPosition.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task WhenReadingTemporalTableThenPeriodColumnsArePopulated()
    {
        var model = await GetDatabaseModelAsync();
        var temporalTable = model.Tables.First(t => t.QualifiedName.Name == "TemporalTask");

        temporalTable.Options.IsTemporalTable.Should().BeTrue();
        temporalTable.Options.HistoryTable.Should().Be(new SchemaQualifiedName { Schema = "dbo", Name = "TemporalTaskHistory" });
        temporalTable.Options.PeriodStartColumnName.Should().Be("ValidFrom");
        temporalTable.Options.PeriodEndColumnName.Should().Be("ValidTo");
    }

    [Fact]
    public async Task WhenReadingSpatialDataTableThenSpatialColumnsAreMapped()
    {
        var model = await GetDatabaseModelAsync();
        var tableNames = model.Tables.Select(t => t.QualifiedName.Name).Order().ToArray();
        var spatialTable = model.Tables.FirstOrDefault(t => t.QualifiedName.Name == "SpatialData");
        spatialTable.Should().NotBeNull($"the schema should include SpatialData; discovered tables: {string.Join(", ", tableNames)}");

        var geometryColumn = spatialTable!.Columns.First(c => c.Name == "GeometryValue");
        geometryColumn.DbType.Should().Be(DbType.Object);
        geometryColumn.SystemType.Should().Be(typeof(object));
        geometryColumn.NativeTypeName.Should().Be("GEOMETRY");
        geometryColumn.Annotations.Should().ContainKey(SqlServerAnnotations.SqlDbType).WhoseValue.Should().Be("Udt");

        var geographyColumn = spatialTable.Columns.First(c => c.Name == "GeographyValue");
        geographyColumn.DbType.Should().Be(DbType.Object);
        geographyColumn.SystemType.Should().Be(typeof(object));
        geographyColumn.NativeTypeName.Should().Be("GEOGRAPHY");
        geographyColumn.Annotations.Should().ContainKey(SqlServerAnnotations.SqlDbType).WhoseValue.Should().Be("Udt");

        var hierarchyColumn = spatialTable.Columns.First(c => c.Name == "HierarchyValue");
        hierarchyColumn.DbType.Should().Be(DbType.Object);
        hierarchyColumn.SystemType.Should().Be(typeof(object));
        hierarchyColumn.NativeTypeName.Should().Be("HIERARCHYID");
        hierarchyColumn.Annotations.Should().ContainKey(SqlServerAnnotations.SqlDbType).WhoseValue.Should().Be("Udt");
    }
}
