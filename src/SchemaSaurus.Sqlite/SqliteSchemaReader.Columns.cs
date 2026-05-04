using Microsoft.Data.Sqlite;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Sqlite;

public sealed partial class SqliteSchemaReader
{
    private static async Task ReadColumnsAsync(
        SqliteConnection connection,
        string tableName,
        TableBuilder tableBuilder,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT name, type, "notnull", dflt_value, pk, hidden
            FROM pragma_table_xinfo($table)
            WHERE hidden IN (0, 2, 3)
            ORDER BY cid
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$table", tableName);

        using var reader = await command.ExecuteReaderAsync(SequentialResultBehavior, cancellationToken).ConfigureAwait(false);

        const int columnNameOrdinal = 0;
        const int typeNameOrdinal = 1;
        const int notNullOrdinal = 2;
        const int defaultValueOrdinal = 3;
        const int primaryKeyOrdinal = 4;
        const int hiddenOrdinal = 5;

        var ordinalPosition = 1;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            // name | type | notnull | dflt_value | pk | hidden
            var columnName = reader.GetString(columnNameOrdinal);
            var typeName = reader.IsDBNull(typeNameOrdinal) ? "" : reader.GetString(typeNameOrdinal);
            var notNull = reader.GetInt32(notNullOrdinal) != 0;
            var defaultValue = reader.IsDBNull(defaultValueOrdinal) ? null : reader.GetString(defaultValueOrdinal);
            var primaryKeyPosition = reader.GetInt32(primaryKeyOrdinal);
            var hidden = reader.GetInt32(hiddenOrdinal); // 0=normal, 1=hidden, 2=generated virtual, 3=generated stored

            var (dbType, systemType) = SqliteTypeMapper.MapNativeType(typeName);

            // Determine if the column is an auto-incrementing identity column.
            var isIdentity = IsRowIdentifier(typeName, primaryKeyPosition);

            // If the type name is empty, SQLite treats it as BLOB affinity.
            var nativeTypeName = string.IsNullOrEmpty(typeName) ? "BLOB" : typeName;

            // hidden values 2 and 3 indicate generated columns; treat both as computed, but only 3 as stored
            var isComputed = hidden is 2 or 3;

            // SQLite does not differentiate between virtual and stored generated columns in the metadata,
            // but we can treat hidden=3 as stored for better compatibility with other providers.
            var isStored = hidden == 3;

            tableBuilder.AddColumn(columnBuilder => columnBuilder
                .WithName(columnName)
                .WithOrdinalPosition(ordinalPosition)
                .WithIsNullable(!notNull)
                .WithDefaultValueSql(defaultValue)
                .WithIsIdentity(isIdentity)
                .WithIsComputed(isComputed)
                .WithIsStored(isStored)
                .WithNativeTypeName(nativeTypeName)
                .WithDbType(dbType)
                .WithSystemType(systemType)
            );

            ordinalPosition++;
        }
    }

    private static bool IsRowIdentifier(string typeName, int pkFlag)
        => pkFlag > 0 && typeName.Equals("INTEGER", StringComparison.OrdinalIgnoreCase);
}
