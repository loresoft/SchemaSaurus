using FluentMigrator;

using SchemaSaurus.Migrator.Providers;

namespace SchemaSaurus.Migrator;

[Migration(2026010216)]
public class CreateDuplicateTable : Migration
{
    private readonly IProviderDefault _providerDefault;

    public CreateDuplicateTable(IProviderDefault providerDefault)
    {
        _providerDefault = providerDefault;
    }

    public string DefaultSchema => _providerDefault.DefaultSchema;

    public override void Up()
    {
        Create.Table("Duplicate")
            .InSchema(DefaultSchema)

            .WithColumn("DuplicateID")
                .AsInt32()
                .Identity()
                .NotNullable()
                .PrimaryKey()

            .WithColumn("DuplicateName")
                .AsAnsiString(50)
                .Nullable()

            .WithColumn("Duplicate_Name")
                .AsAnsiString(50)
                .Nullable()

            .WithColumn("Duplicate")
                .AsAnsiString(50)
                .Nullable();
    }

    public override void Down()
    {
        Delete.Table("Duplicate").InSchema(DefaultSchema);
    }
}
