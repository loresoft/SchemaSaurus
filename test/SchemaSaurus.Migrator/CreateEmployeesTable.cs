using FluentMigrator;

using SchemaSaurus.Migrator.Providers;

namespace SchemaSaurus.Migrator;

[Migration(2026010214)]
public class CreateEmployeesTable : Migration
{
    private readonly IProviderDefault _providerDefault;

    public CreateEmployeesTable(IProviderDefault providerDefault)
    {
        _providerDefault = providerDefault;
    }

    public string DefaultSchema => _providerDefault.DefaultSchema;
    public bool SupportForeignKeys => _providerDefault.SupportForeignKeys;

    public override void Up()
    {
        Create.Table("Employees")
            .InSchema(DefaultSchema)

            .WithColumn("EmployeeId")
                .AsInt32()
                .Identity()
                .NotNullable()
                .PrimaryKey()

            .WithColumn("FirstName")
                .AsAnsiString(50)
                .NotNullable()

            .WithColumn("LastName")
                .AsAnsiString(50)
                .NotNullable()

            .WithColumn("Email")
                .AsAnsiString(50)
                .Nullable()

            .WithColumn("ManagerId")
                .AsInt32()
                .Nullable()

            .WithColumn("CreatedBy")
                .AsInt32()
                .NotNullable()

            .WithColumn("UpdatedBy")
                .AsInt32()
                .NotNullable();

        if (!SupportForeignKeys)
            return;

        Create.ForeignKey("FK_Employees_Employees_ManagerId")
            .FromTable("Employees").InSchema(DefaultSchema).ForeignColumn("ManagerId")
            .ToTable("Employees").InSchema(DefaultSchema).PrimaryColumn("EmployeeId");

        Create.ForeignKey("FK_Employees_Employees_CreatedBy")
            .FromTable("Employees").InSchema(DefaultSchema).ForeignColumn("CreatedBy")
            .ToTable("Employees").InSchema(DefaultSchema).PrimaryColumn("EmployeeId");

        Create.ForeignKey("FK_Employees_Employees_UpdatedBy")
            .FromTable("Employees").InSchema(DefaultSchema).ForeignColumn("UpdatedBy")
            .ToTable("Employees").InSchema(DefaultSchema).PrimaryColumn("EmployeeId");
    }

    public override void Down()
    {
        Delete.Table("Employees").InSchema(DefaultSchema);
    }
}
