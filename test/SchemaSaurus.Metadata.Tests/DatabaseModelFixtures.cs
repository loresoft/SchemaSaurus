using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests;

/// <summary>
/// Reusable factory methods for building <see cref="DatabaseModel"/> instances used across tests.
/// </summary>
internal static class DatabaseModelFixtures
{
    /// <summary>
    /// Builds a rich <see cref="DatabaseModel"/> containing representative objects of every type
    /// (tables with columns, indexes, constraints, foreign keys, triggers; views; sequences;
    /// stored procedures; scalar and table-valued functions; user-defined types).
    /// </summary>
    public static DatabaseModel CreateFullModel()
    {
        return new DatabaseModelBuilder()
            .WithDatabaseName("AdventureWorks")
            .WithProvider("SqlServer")
            .WithCollation("SQL_Latin1_General_CP1_CI_AS")
            .WithEdition("Developer Edition")
            .WithCompatibilityLevel("160")
            .WithDefaultSchemaName("dbo")
            .WithServerVersion("16.0.1135.2")
            .AddTable(t => t
                .WithQualifiedName("dbo", "Customers")
                .WithDescription("Customer master table")
                .WithPrimaryKey("PK_Customers", true,
                    new ColumnReference { ColumnName = "Id" })
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))
                    .WithIsIdentity(true)
                    .WithIdentitySeed(1)
                    .WithIdentityIncrement(1))
                .AddColumn(c => c
                    .WithName("Name")
                    .WithOrdinalPosition(2)
                    .WithIsNullable(false)
                    .WithDbType(DbType.String)
                    .WithNativeTypeName("nvarchar(200)")
                    .WithSystemType(typeof(string))
                    .WithMaxLength(200)
                    .WithIsUnicode(true)
                    .WithIsFixedLength(false))
                .AddColumn(c => c
                    .WithName("Email")
                    .WithOrdinalPosition(3)
                    .WithIsNullable(true)
                    .WithDbType(DbType.String)
                    .WithNativeTypeName("nvarchar(256)")
                    .WithSystemType(typeof(string))
                    .WithMaxLength(256)
                    .WithIsUnicode(true)
                    .WithIsFixedLength(false))
                .AddUniqueConstraint("UQ_Customers_Email",
                    new ColumnReference { ColumnName = "Email" })
                .AddIndex(ix => ix
                    .WithName("IX_Customers_Name")
                    .AddColumn("Name")))
            .AddTable(t => t
                .WithQualifiedName("dbo", "Orders")
                .WithDescription("Sales order header")
                .WithPrimaryKey("PK_Orders", true,
                    new ColumnReference { ColumnName = "Id" })
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))
                    .WithIsIdentity(true)
                    .WithIdentitySeed(1)
                    .WithIdentityIncrement(1))
                .AddColumn(c => c
                    .WithName("CustomerId")
                    .WithOrdinalPosition(2)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int)))
                .AddColumn(c => c
                    .WithName("OrderDate")
                    .WithOrdinalPosition(3)
                    .WithIsNullable(false)
                    .WithDbType(DbType.DateTime)
                    .WithNativeTypeName("datetime2")
                    .WithSystemType(typeof(DateTime))
                    .WithDefaultValueSql("(getdate())"))
                .AddColumn(c => c
                    .WithName("TotalAmount")
                    .WithOrdinalPosition(4)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Decimal)
                    .WithNativeTypeName("decimal(18,2)")
                    .WithSystemType(typeof(decimal))
                    .WithPrecision(18)
                    .WithScale(2))
                .AddColumn(c => c
                    .WithName("RowVersion")
                    .WithOrdinalPosition(5)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Binary)
                    .WithNativeTypeName("rowversion")
                    .WithSystemType(typeof(byte[]))
                    .WithIsRowVersion(true)
                    .WithIsConcurrencyToken(true))
                .AddForeignKey(fk => fk
                    .WithName("FK_Orders_Customers")
                    .WithPrincipalTableName("dbo", "Customers")
                    .AddColumnMapping("CustomerId", "Id")
                    .WithOnDelete(ReferentialAction.Cascade))
                .AddCheckConstraint("CK_Orders_TotalAmount", "([TotalAmount] >= 0)")
                .AddIndex(ix => ix
                    .WithName("IX_Orders_CustomerId")
                    .AddColumn("CustomerId")
                    .AddIncludedColumn("OrderDate"))
                .AddTrigger(new Trigger
                {
                    Name = "TR_Orders_Audit",
                    Timing = TriggerTiming.After,
                    Events = TriggerEvent.Insert | TriggerEvent.Update,
                    Definition = "CREATE TRIGGER TR_Orders_Audit ON dbo.Orders AFTER INSERT, UPDATE AS BEGIN ... END",
                }))
            .AddView(v => v
                .WithQualifiedName("dbo", "vw_ActiveCustomers")
                .WithDefinition("SELECT Id, Name, Email FROM dbo.Customers WHERE Email IS NOT NULL")
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int)))
                .AddColumn(c => c
                    .WithName("Name")
                    .WithOrdinalPosition(2)
                    .WithIsNullable(false)
                    .WithDbType(DbType.String)
                    .WithNativeTypeName("nvarchar(200)")
                    .WithSystemType(typeof(string))))
            .AddSequence(s => s
                .WithQualifiedName("dbo", "InvoiceSeq")
                .WithDbType(DbType.Int64)
                .WithSystemType(typeof(long))
                .WithStartValue(1000)
                .WithIncrement(1)
                .WithMinValue(1)
                .WithMaxValue(long.MaxValue)
                .WithIsCycling(false)
                .WithCacheSize(50))
            .AddStoredProcedure(sp => sp
                .WithQualifiedName("dbo", "uspGetCustomerOrders")
                .WithDescription("Returns all orders for a customer")
                .WithDefinition("CREATE PROCEDURE dbo.uspGetCustomerOrders @CustomerId int AS SELECT * FROM Orders WHERE CustomerId = @CustomerId")
                .AddParameter(p => p
                    .WithName("@CustomerId")
                    .WithOrdinal(1)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))))
            .AddScalarFunction(fn => fn
                .WithQualifiedName("dbo", "fnGetOrderTotal")
                .WithReturnType(DbType.Decimal, "decimal(18,2)", typeof(decimal))
                .WithIsDeterministic(true)
                .WithDefinition("CREATE FUNCTION dbo.fnGetOrderTotal(@OrderId int) RETURNS decimal(18,2) AS BEGIN RETURN 0 END")
                .AddParameter(p => p
                    .WithName("@OrderId")
                    .WithOrdinal(1)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))))
            .AddTableValuedFunction(fn => fn
                .WithQualifiedName("dbo", "fnGetOrdersByDate")
                .WithDefinition("CREATE FUNCTION dbo.fnGetOrdersByDate(@StartDate datetime2) RETURNS TABLE AS RETURN SELECT * FROM Orders WHERE OrderDate >= @StartDate")
                .AddParameter(p => p
                    .WithName("@StartDate")
                    .WithOrdinal(1)
                    .WithDbType(DbType.DateTime)
                    .WithNativeTypeName("datetime2")
                    .WithSystemType(typeof(DateTime)))
                .AddReturnColumn("Id", 1, DbType.Int32, "int", typeof(int))
                .AddReturnColumn("OrderDate", 2, DbType.DateTime, "datetime2", typeof(DateTime))
                .AddReturnColumn("TotalAmount", 3, DbType.Decimal, "decimal(18,2)", typeof(decimal)))
            .AddUserDefinedType(udt => udt
                .WithQualifiedName("dbo", "PhoneNumber")
                .WithKind(UserDefinedTypeKind.Alias)
                .WithDbType(DbType.String)
                .WithNativeTypeName("nvarchar(20)")
                .WithSystemType(typeof(string))
                .WithMaxLength(20)
                .WithIsUnicode(true))
            .Build();
    }

    /// <summary>
    /// Builds a minimal <see cref="DatabaseModel"/> with only required properties and empty collections.
    /// </summary>
    public static DatabaseModel CreateMinimalModel()
    {
        return new DatabaseModelBuilder()
            .WithDatabaseName("EmptyDb")
            .WithProvider("Sqlite")
            .Build();
    }
}
