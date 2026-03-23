using FluentMigrator;

using SchemaSaurus.Migrator.Providers;

namespace SchemaSaurus.Migrator;

[Migration(2026010213)]
public class CreateGuidAutoTable : Migration
{
    private readonly IProviderDefault _providerDefault;

    public CreateGuidAutoTable(IProviderDefault providerDefault)
    {
        _providerDefault = providerDefault;
    }

    public string DefaultSchema => _providerDefault.DefaultSchema;

    public string RowVersionType => _providerDefault.RowVersionType;

    public override void Up()
    {
        Create.Table("GuidAuto")
            .InSchema(DefaultSchema)

            .WithColumn("GuidID")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()

            .WithColumn("AutoID")
                .AsInt32()
                .Identity()
                .NotNullable()

            .WithColumn("Name")
                .AsAnsiString(50)
                .NotNullable()

            .WithColumn("Flag")
                .AsCustom(RowVersionType)
                .NotNullable();
    }

    public override void Down()
    {
        Delete.Table("GuidAuto").InSchema(DefaultSchema);
    }
}
