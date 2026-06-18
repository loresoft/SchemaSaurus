using SchemaSaurus.Metadata;
using SchemaSaurus.Metadata.Provider;
using SchemaSaurus.MySql;
using SchemaSaurus.Oracle;
using SchemaSaurus.PostgreSql;
using SchemaSaurus.Sqlite;
using SchemaSaurus.SqlServer;

using Spectre.Console;
using Spectre.Console.Cli;

namespace SchemaSaurus;

internal sealed class ExportCommand : AsyncCommand<ExportCommandSettings>
{
    private const int Success = 0;
    private const int Failure = 1;
    private const int UnknownProvider = 2;

    private static readonly string[] ProviderNames = ["SqlServer", "PostgreSQL", "MySQL", "Oracle", "SQLite",];
    private static readonly IAnsiConsole ErrorConsole = CreateErrorConsole();


    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        ExportCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var provider = settings.Provider!.Trim();
        var reader = CreateReader(provider);

        if (reader is null)
        {
            WriteUnknownProviderError(provider);
            return UnknownProvider;
        }

        try
        {
            var schemaReaderOptions = CreateOptions(settings);

            var model = await reader
                .ReadAsync(settings.ConnectionString!, schemaReaderOptions, cancellationToken)
                .ConfigureAwait(false);

            var json = model.ToJson();

            if (string.IsNullOrWhiteSpace(settings.Output))
            {
                await Console.Out.WriteLineAsync(json).ConfigureAwait(false);
                return Success;
            }

            var outputPath = Path.GetFullPath(settings.Output);
            var outputDirectory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            await File.WriteAllTextAsync(outputPath, json, cancellationToken).ConfigureAwait(false);

            AnsiConsole.MarkupLine($"Exported schema metadata to [green]{outputPath.EscapeMarkup()}[/].");

            return Success;
        }
        catch (Exception ex)
        {
            var message = ex.Message.EscapeMarkup();
            ErrorConsole.MarkupLine($"[red]Export failed:[/] {message}");
            return Failure;
        }
    }


    private static IAnsiConsole CreateErrorConsole()
    {
        var output = new AnsiConsoleOutput(Console.Error);
        var settings = new AnsiConsoleSettings
        {
            Out = output,
        };

        return AnsiConsole.Create(settings);
    }

    private static IDatabaseSchemaReader? CreateReader(string provider)
    {
        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            return new SqlServerSchemaReader();

        if (string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return new PostgreSqlSchemaReader();

        if (string.Equals(provider, "MySQL", StringComparison.OrdinalIgnoreCase))
            return new MySqlSchemaReader();

        if (string.Equals(provider, "Oracle", StringComparison.OrdinalIgnoreCase))
            return new OracleSchemaReader();

        if (string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase))
            return new SqliteSchemaReader();

        return null;
    }

    private static void WriteUnknownProviderError(string provider)
    {
        var providerNames = ProviderNames.Select(providerName => providerName.EscapeMarkup());
        var availableProviders = string.Join(", ", providerNames);
        var providerMarkup = provider.EscapeMarkup();

        ErrorConsole.MarkupLine($"[red]Unknown provider[/] [yellow]'{providerMarkup}'[/]. Available providers: [green]{availableProviders}[/].");
    }

    private static SchemaReaderOptions CreateOptions(ExportCommandSettings settings)
    {
        return new SchemaReaderOptions
        {
            Schemas = CleanValues(settings.Schemas),
            Tables = CleanValues(settings.Tables),
            IncludeViews = !settings.ExcludeViews,
            IncludeStoredProcedures = !settings.ExcludeStoredProcedures,
            IncludeScalarFunctions = !settings.ExcludeScalarFunctions,
            IncludeTableValuedFunctions = !settings.ExcludeTableValuedFunctions,
            IncludeSequences = !settings.ExcludeSequences,
            IncludeUserDefinedTypes = !settings.ExcludeUserDefinedTypes,
        };
    }

    private static string[] CleanValues(IEnumerable<string>? values)
    {
        if (values is null)
            return [];

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();
    }
}
