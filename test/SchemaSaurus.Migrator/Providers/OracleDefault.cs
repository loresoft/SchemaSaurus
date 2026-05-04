namespace SchemaSaurus.Migrator.Providers;

public class OracleDefault : IProviderDefault
{
    public string DefaultSchema { get; set; } = string.Empty;
    public string RowVersionType { get; set; } = "TIMESTAMP";
    public string DateTimeOffsetType { get; set; } = "TIMESTAMP WITH TIME ZONE";

    public bool SupportTemporalTable { get; set; } = false;
    public bool SupportChangeTracking { get; set; } = false;
    public bool SupportSchema { get; set; } = true;
    public bool SupportSequences { get; set; } = true;
    public bool SupportForeignKeys { get; set; } = true;
}
