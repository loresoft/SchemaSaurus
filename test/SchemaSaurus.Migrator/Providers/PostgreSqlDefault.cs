namespace SchemaSaurus.Migrator.Providers;

public class PostgreSqlDefault : IProviderDefault
{
    public string DefaultSchema { get; set; } = "public";
    public string RowVersionType { get; set; } = "XID";
    public string DateTimeOffsetType { get; set; } = "TIMESTAMPTZ";

    public bool SupportTemporalTable { get; set; } = false;
    public bool SupportChangeTracking { get; set; } = false;
    public bool SupportSchema { get; set; } = true;
    public bool SupportSequences { get; set; } = true;
}
