using System.Data;

using SchemaSaurus.PostgreSql.Tests.Fixtures;
using SchemaSaurus.PostgreSql;

namespace SchemaSaurus.PostgreSql.Tests;

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

        model.Tables.Should().Contain(t => t.SchemaQualifiedName.Name == "User");
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenStatusTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.SchemaQualifiedName.Name == "Status");
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenTaskTableExists()
    {
        var model = await GetDatabaseModelAsync();

        model.Tables.Should().Contain(t => t.SchemaQualifiedName.Name == "Task");
    }

    [Fact]
    public async Task WhenReadingSchemaTablesThenTablesHavePublicSchema()
    {
        var model = await GetDatabaseModelAsync();

        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");
        userTable.SchemaQualifiedName.Schema.Should().Be("public");
    }

    [Fact]
    public async Task WhenReadingUserTableThenColumnsAreDiscovered()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

        userTable.Columns.Should().NotBeEmpty();
        userTable.Columns.Should().Contain(c => c.Name == "Id");
        userTable.Columns.Should().Contain(c => c.Name == "UserName");
        userTable.Columns.Should().Contain(c => c.Name == "EmailAddress");
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnIsIdentity()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

        var idColumn = userTable.Columns.First(c => c.Name == "Id");
        idColumn.IsIdentity.Should().BeTrue();
        idColumn.IdentitySeed.Should().Be(1);
        idColumn.IdentityIncrement.Should().Be(1);
    }

    [Fact]
    public async Task WhenReadingUserTableThenIdColumnIsInt32()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

        var idColumn = userTable.Columns.First(c => c.Name == "Id");
        idColumn.DbType.Should().Be(DbType.Int32);
        idColumn.SystemType.Should().Be(typeof(int));

        idColumn
            .Annotations.Should().ContainKey(PostgreSqlAnnotations.NpgsqlDbType)
            .WhoseValue.Should().Be("Integer");
    }

    [Fact]
    public async Task WhenReadingUserTableThenUserNameIsString()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

        var userNameColumn = userTable.Columns.First(c => c.Name == "UserName");
        userNameColumn.DbType.Should().Be(DbType.String);
        userNameColumn.IsUnicode.Should().Be(true);
        userNameColumn.MaxLength.Should().Be(50);
        userNameColumn.IsNullable.Should().BeFalse();

        userNameColumn
            .Annotations.Should().ContainKey(PostgreSqlAnnotations.NpgsqlDbType)
            .WhoseValue.Should().Be("Varchar");
    }

    [Fact]
    public async Task WhenReadingUserTableThenEmailAddressIsString()
    {
        var model = await GetDatabaseModelAsync();
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

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
        var userTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "User");

        var firstNameColumn = userTable.Columns.First(c => c.Name == "FirstName");
        firstNameColumn.IsNullable.Should().BeTrue();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenRowVersionColumnExists()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "Status");

        var rowVersionColumn = statusTable.Columns.First(c => c.Name == "RowVersion");
        rowVersionColumn.DbType.Should().Be(DbType.Object);
    }

    [Fact]
    public async Task WhenReadingStatusTableThenDefaultValueSqlIsPopulated()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "Status");

        var displayOrderColumn = statusTable.Columns.First(c => c.Name == "DisplayOrder");
        displayOrderColumn.DefaultValueSql.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WhenReadingStatusTableThenIsActiveHasDefaultValue()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "Status");

        var isActiveColumn = statusTable.Columns.First(c => c.Name == "IsActive");
        isActiveColumn.DefaultValueSql.Should().NotBeNullOrWhiteSpace();
        isActiveColumn.DbType.Should().Be(DbType.Boolean);
    }

    [Fact]
    public async Task WhenReadingTaskTableThenGuidPrimaryKeyExists()
    {
        var model = await GetDatabaseModelAsync();
        var taskTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "Task");

        var idColumn = taskTable.Columns.First(c => c.Name == "Id");
        idColumn.DbType.Should().Be(DbType.Guid);
        idColumn.SystemType.Should().Be(typeof(Guid));
        idColumn.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task WhenReadingDataTypeTableThenVariousTypesAreMapped()
    {
        var model = await GetDatabaseModelAsync();
        var dataTypeTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "DataType");

        dataTypeTable.Columns.First(c => c.Name == "Boolean").DbType.Should().Be(DbType.Boolean);
        dataTypeTable.Columns.First(c => c.Name == "Short").DbType.Should().Be(DbType.Int16);
        dataTypeTable.Columns.First(c => c.Name == "Long").DbType.Should().Be(DbType.Int64);
        dataTypeTable.Columns.First(c => c.Name == "Float").DbType.Should().Be(DbType.Single);
        dataTypeTable.Columns.First(c => c.Name == "Double").DbType.Should().Be(DbType.Double);
        dataTypeTable.Columns.First(c => c.Name == "Decimal").DbType.Should().Be(DbType.Decimal);
        dataTypeTable.Columns.First(c => c.Name == "DateTime").DbType.Should().Be(DbType.DateTime2);
        dataTypeTable.Columns.First(c => c.Name == "DateTimeOffset").DbType.Should().Be(DbType.DateTimeOffset);
        dataTypeTable.Columns.First(c => c.Name == "Guid").DbType.Should().Be(DbType.Guid);
    }

    [Fact]
    public async Task WhenReadingDataTypeTableThenDecimalHasPrecisionAndScale()
    {
        var model = await GetDatabaseModelAsync();
        var dataTypeTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "DataType");

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
        model.Tables[0].SchemaQualifiedName.Name.Should().Be("Status");
    }

    [Fact]
    public async Task WhenReadingColumnsOrdinalPositionsArePopulated()
    {
        var model = await GetDatabaseModelAsync();
        var statusTable = model.Tables.First(t => t.SchemaQualifiedName.Name == "Status");

        statusTable.Columns.Should().AllSatisfy(c => c.OrdinalPosition.Should().BeGreaterThan(0));
    }
}
