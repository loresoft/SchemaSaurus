using FluentMigrator;

namespace SchemaSaurus.SqlServer.Tests.Migrations;

[Migration(2026020104)]
public class AddSpatialDataScript : Migration
{
    public override void Up()
    {
        Execute.EmbeddedScript("SchemaSaurus.SqlServer.Tests.Scripts.Script060.SpatialData.sql");
    }

    public override void Down()
    {
    }
}
