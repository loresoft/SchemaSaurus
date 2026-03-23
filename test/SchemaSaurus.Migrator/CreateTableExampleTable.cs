using FluentMigrator;

using SchemaSaurus.Migrator.Providers;

namespace SchemaSaurus.Migrator;

[Migration(2026010204)]
public class CreateTableExampleTable : Migration
{
    private readonly IProviderDefault _providerDefault;

    public CreateTableExampleTable(IProviderDefault providerDefault)
    {
        _providerDefault = providerDefault;
    }

    public string DefaultSchema => _providerDefault.DefaultSchema;

    public override void Up()
    {
        Create.Table("Table Example")
            .InSchema(DefaultSchema)

            .WithColumn("Table Example ID")
                .AsInt32()
                .Identity()
                .NotNullable()
                .PrimaryKey()

            .WithColumn("Name Example")
                .AsAnsiString(50)
                .NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Table Example").InSchema(DefaultSchema);
    }
}
