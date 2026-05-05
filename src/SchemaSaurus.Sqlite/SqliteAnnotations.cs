namespace SchemaSaurus.Sqlite;

/// <summary>
/// SQLite provider-specific annotation keys stored on <see cref="SchemaSaurus.Metadata.DatabaseModel.Annotations"/>.
/// Values correspond to SQLite PRAGMA settings read during schema discovery.
/// </summary>
public static class SqliteAnnotations
{
    /// <summary>The application ID set via <c>PRAGMA application_id</c>.</summary>
    public const string ApplicationId = "application_id";

    /// <summary>The auto-vacuum mode (<c>PRAGMA auto_vacuum</c>).</summary>
    public const string AutoVacuum = "auto_vacuum";

    /// <summary>The suggested cache size in pages (<c>PRAGMA cache_size</c>).</summary>
    public const string CacheSize = "cache_size";

    /// <summary>Whether foreign key enforcement is enabled (<c>PRAGMA foreign_keys</c>).</summary>
    public const string ForeignKeys = "foreign_keys";

    /// <summary>The journal mode (<c>PRAGMA journal_mode</c>), e.g., <c>"wal"</c>, <c>"delete"</c>.</summary>
    public const string JournalMode = "journal_mode";

    /// <summary>The locking mode (<c>PRAGMA locking_mode</c>), e.g., <c>"normal"</c>, <c>"exclusive"</c>.</summary>
    public const string LockingMode = "locking_mode";

    /// <summary>Total number of pages in the database file (<c>PRAGMA page_count</c>).</summary>
    public const string PageCount = "page_count";

    /// <summary>The database page size in bytes (<c>PRAGMA page_size</c>).</summary>
    public const string PageSize = "page_size";

    /// <summary>The synchronous mode (<c>PRAGMA synchronous</c>), e.g., <c>"normal"</c>, <c>"full"</c>.</summary>
    public const string Synchronous = "synchronous";

    /// <summary>Where temporary tables and indexes are stored (<c>PRAGMA temp_store</c>).</summary>
    public const string TempStore = "temp_store";

    /// <summary>The user version integer (<c>PRAGMA user_version</c>).</summary>
    public const string UserVersion = "user_version";

    /// <summary>The WAL auto-checkpoint interval in pages (<c>PRAGMA wal_autocheckpoint</c>).</summary>
    public const string AutoCheckpoint = "wal_autocheckpoint";
}
