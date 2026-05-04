using FluentMigrator;

using SchemaSaurus.Migrator.Extensions;
using SchemaSaurus.Migrator.Providers;

namespace SchemaSaurus.Migrator;

[Migration(2026010202)]
public class CreateTemplateTable : Migration
{
    private readonly IProviderDefault _providerDefault;

    public CreateTemplateTable(IProviderDefault providerDefault)
    {
        _providerDefault = providerDefault;
    }

    public string DefaultSchema => _providerDefault.DefaultSchema;
    public bool SupportIdentity => _providerDefault.SupportIdentity;

    public override void Up()
    {
        Create.Table("Template")
            .InSchema(DefaultSchema)

            .WithColumn("TemplateID")
                .AsInt32()
                .IdentityIf(SupportIdentity)
                .NotNullable()
                .PrimaryKey()

            .WithColumn("TemplateName")
                .AsAnsiString(50)
                .Nullable();
    }

    public override void Down()
    {
        Delete.Table("Template").InSchema(DefaultSchema);
    }
}
