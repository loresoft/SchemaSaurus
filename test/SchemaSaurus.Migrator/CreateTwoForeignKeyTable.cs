using FluentMigrator;

using SchemaSaurus.Migrator.Providers;

namespace SchemaSaurus.Migrator;

[Migration(2026010221)]
public class CreateTwoForeignKeyTable : Migration
{
    private readonly IProviderDefault _providerDefault;

    public CreateTwoForeignKeyTable(IProviderDefault providerDefault)
    {
        _providerDefault = providerDefault;
    }

    public string DefaultSchema => _providerDefault.DefaultSchema;
    public bool SupportForeignKeys => _providerDefault.SupportForeignKeys;

    public override void Up()
    {
        Create.Table("TwoForeignKey")
            .InSchema(DefaultSchema)

            .WithColumn("Id")
                .AsInt32()
                .Identity()
                .NotNullable()
                .PrimaryKey()

            .WithColumn("FirstName")
                .AsString(50)
                .NotNullable()

            .WithColumn("LastName")
                .AsString(50)
                .NotNullable()

            .WithColumn("Address")
                .AsString(50)
                .Nullable()

            .WithColumn("Blah")
                .AsString(50)
                .Nullable();

        if (!SupportForeignKeys)
            return;

        Create.ForeignKey("FK_TwoForeignKey_TwoKey")
            .FromTable("TwoForeignKey").InSchema(DefaultSchema).ForeignColumns("FirstName", "LastName")
            .ToTable("TwoKey").InSchema(DefaultSchema).PrimaryColumns("FirstName", "LastName");
    }

    public override void Down()
    {
        Delete.Table("TwoForeignKey").InSchema(DefaultSchema);
    }
}
