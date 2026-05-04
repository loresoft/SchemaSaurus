using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Builders;
using SchemaSaurus.Metadata.Extensions;

namespace SchemaSaurus.Sqlite;

public sealed partial class SqliteSchemaReader
{
    private static async Task ReadTriggersAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name, sql
            FROM sqlite_master
            WHERE type = 'trigger' AND tbl_name = $tableName
            ORDER BY name
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$tableName", tableName);

        // SQLite stores trigger metadata in sqlite_master, including the original CREATE TRIGGER SQL.
        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int triggerNameOrdinal = 0;
        const int sqlOrdinal = 1;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var triggerName = reader.GetString(triggerNameOrdinal);
            var triggerSql = reader.GetStringNull(sqlOrdinal);

            // SQLite does not expose timing or events as separate columns, so infer them from the definition.
            var (timing, events) = ParseTriggerSql(triggerSql);

            var trigger = new Trigger
            {
                Name = triggerName,
                Timing = timing,
                Events = events,
                Definition = triggerSql,
            };

            tableBuilder.AddTrigger(trigger);
        }
    }

    private static (TriggerTiming Timing, TriggerEvent Events) ParseTriggerSql(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return (TriggerTiming.Before, TriggerEvent.None);

        var upper = sql!.ToUpperInvariant();

        // BEFORE is SQLite's default when no timing keyword is present.
        var timing = TriggerTiming.Before;
        if (upper.Contains("INSTEAD OF"))
            timing = TriggerTiming.InsteadOf;
        else if (upper.Contains("AFTER"))
            timing = TriggerTiming.After;

        var events = TriggerEvent.None;
        if (upper.Contains("INSERT"))
            events |= TriggerEvent.Insert;
        if (upper.Contains("UPDATE"))
            events |= TriggerEvent.Update;
        if (upper.Contains("DELETE"))
            events |= TriggerEvent.Delete;

        return (timing, events);
    }
}
