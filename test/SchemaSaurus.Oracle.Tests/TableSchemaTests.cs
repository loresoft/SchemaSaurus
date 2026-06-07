using System.Data;

using SchemaSaurus.Oracle.Tests.Fixtures;

namespace SchemaSaurus.Oracle.Tests;

public class TableSchemaTests(DatabaseFixture databaseFixture)
    : SchemaReaderTestBase(databaseFixture)
{
    private const string UserTableName = "User";
    private const string StatusTableName = "STATUS";
    private const string TaskTableName = "TASK";
    private const string DataTypeTableName = "DATATYPE";

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

        model.Tables.Should().Contain(t => t.QualifiedName.Name == UserTableName);
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenStatusTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.QualifiedName.Name == StatusTableName);
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenTaskTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.QualifiedName.Name == TaskTableName);
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenTablesHaveDefaultSchema()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);
        userTable.QualifiedName.Schema.Should().Be(model.DefaultSchemaName);
    }

    [Fact]
    public async Task WhenReadingUserTableThenColumnsAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);

        userTable.Columns.Should().NotBeEmpty();
        userTable.Columns.Should().Contain(c => c.Name == "ID");
        userTable.Columns.Should().Contain(c => c.Name == "USERNAME");
        userTable.Columns.Should().Contain(c => c.Name == "EMAILADDRESS");
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnIsNotIdentity()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);

        var idColumn = userTable.Columns.First(c => c.Name == "ID");
        idColumn.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnIsInt32()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);

        var idColumn = userTable.Columns.First(c => c.Name == "ID");
        idColumn.DbType.Should().Be(DbType.Int32);
        idColumn.SystemType.Should().Be(typeof(int));

        idColumn
            .Annotations.Should().ContainKey(OracleAnnotations.OracleDbType)
            .WhoseValue.Should().Be("Int32");
    }

    [Fact]
    public async Task WhenReadingInt32ColumnThenPrecisionAndScaleAreNotSet()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);

        var idColumn = userTable.Columns.First(c => c.Name == "ID");
        idColumn.Precision.Should().BeNull();
        idColumn.Scale.Should().BeNull();
    }

    [Fact]
    public async Task WhenReadingUserTableThenUserNameIsAnsiString()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);

        var userNameColumn = userTable.Columns.First(c => c.Name == "USERNAME");
        userNameColumn.DbType.Should().Be(DbType.AnsiString);
        userNameColumn.IsUnicode.Should().Be(false);
        userNameColumn.MaxLength.Should().Be(50);
        userNameColumn.IsNullable.Should().BeFalse();

        userNameColumn
            .Annotations.Should().ContainKey(OracleAnnotations.OracleDbType)
            .WhoseValue.Should().Be("Varchar2");
    }

    [Fact]
    public async Task WhenReadingUserTableThenEmailAddressIsUnicodeString()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);

        var emailColumn = userTable.Columns.First(c => c.Name == "EMAILADDRESS");
        emailColumn.DbType.Should().Be(DbType.String);
        emailColumn.IsUnicode.Should().Be(true);
        emailColumn.MaxLength.Should().Be(256);
        emailColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingUserTableThenNullableColumnIsMarkedNullable()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.QualifiedName.Name == UserTableName);

        var firstNameColumn = userTable.Columns.First(c => c.Name == "FIRSTNAME");
        firstNameColumn.IsNullable.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenRowVersionColumnExists()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == StatusTableName);

        var rowVersionColumn = statusTable.Columns.First(c => c.Name == "ROWVERSION");
        rowVersionColumn.DbType.Should().Be(DbType.DateTime2);
        rowVersionColumn.SystemType.Should().Be(typeof(DateTime));
    }

    [Fact]
    public async Task WhenReadingStatusTableThenDefaultValueSqlIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == StatusTableName);

        var displayOrderColumn = statusTable.Columns.First(c => c.Name == "DISPLAYORDER");
        displayOrderColumn.DefaultValueSql.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenIsActiveHasDefaultValue()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == StatusTableName);

        var isActiveColumn = statusTable.Columns.First(c => c.Name == "ISACTIVE");
        isActiveColumn.DefaultValueSql.Should().BeNullOrWhiteSpace();
        isActiveColumn.DbType.Should().Be(DbType.Boolean);
    }

    [Fact]
    public async Task WhenReadingTaskTableThenGuidPrimaryKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.QualifiedName.Name == TaskTableName);

        var idColumn = taskTable.Columns.First(c => c.Name == "ID");
        idColumn.DbType.Should().Be(DbType.Guid);
        idColumn.SystemType.Should().Be(typeof(Guid));
        idColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingDataTypeTableThenVariousTypesAreMapped()
    {
        var model = await GetDatabaseModelAsync();
        var dataTypeTable = model.Tables.First(t => t.QualifiedName.Name == DataTypeTableName);

        dataTypeTable.Columns.First(c => c.Name == "BOOLEAN").DbType.Should().Be(DbType.Boolean);
        dataTypeTable.Columns.First(c => c.Name == "SHORT").DbType.Should().Be(DbType.Int16);
        dataTypeTable.Columns.First(c => c.Name == "Long").DbType.Should().Be(DbType.Int64);
        dataTypeTable.Columns.First(c => c.Name == "Float").DbType.Should().Be(DbType.Double);
        dataTypeTable.Columns.First(c => c.Name == "DOUBLE").DbType.Should().Be(DbType.Double);
        dataTypeTable.Columns.First(c => c.Name == "Decimal").DbType.Should().Be(DbType.Decimal);
        dataTypeTable.Columns.First(c => c.Name == "DATETIME").DbType.Should().Be(DbType.DateTime2);
        dataTypeTable.Columns.First(c => c.Name == "DATETIMEOFFSET").DbType.Should().Be(DbType.DateTime2);
        dataTypeTable.Columns.First(c => c.Name == "GUID").DbType.Should().Be(DbType.Guid);
    }

    [Fact]
    public async Task WhenReadingDataTypeTableThenDecimalHasPrecisionAndScale()
    {
        var model = await GetDatabaseModelAsync();
        var dataTypeTable = model.Tables.First(t => t.QualifiedName.Name == DataTypeTableName);

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
        model.Tables[0].QualifiedName.Name.Should().Be(StatusTableName);
    }

    [Fact]
    public async Task WhenReadingColumnsOrdinalPositionsArePopulated()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.QualifiedName.Name == StatusTableName);

        statusTable.Columns.Should().AllSatisfy(c => c.OrdinalPosition.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task WhenReadingSpatialDataTableThenSpatialColumnsAreMapped()
    {
        var model = await GetDatabaseModelAsync();
        var spatialTable = model.Tables.First(t => t.QualifiedName.Name == "SPATIALDATA");

        var geometryColumn = spatialTable.Columns.First(c => c.Name == "GEOMETRYVALUE");
        geometryColumn.DbType.Should().Be(DbType.Object);
        geometryColumn.SystemType.Should().Be(typeof(object));
        geometryColumn.NativeTypeName.Should().Be("SDO_GEOMETRY");
        geometryColumn.Annotations.Should().ContainKey(OracleAnnotations.OracleDbType).WhoseValue.Should().Be("Object");
    }
}
