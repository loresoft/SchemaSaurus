namespace SchemaSaurus.Sqlite;

/// <summary>
/// SQLite provider-specific annotation keys.
/// </summary>
public static class SqliteAnnotations
{
    public const string ApplicationId = "application_id";
    public const string AutoVacuum = "auto_vacuum";
    public const string CacheSize = "cache_size";
    public const string ForeignKeys = "foreign_keys";
    public const string JournalMode = "journal_mode";
    public const string LockingMode = "locking_mode";
    public const string PageCount = "page_count";
    public const string PageSize = "page_size";
    public const string Synchronous = "synchronous";
    public const string TempStore = "temp_store";
    public const string UserVersion = "user_version";
    public const string AutoCheckpoint = "wal_autocheckpoint";
}
