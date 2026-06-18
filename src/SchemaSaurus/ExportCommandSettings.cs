using Spectre.Console;
using Spectre.Console.Cli;

namespace SchemaSaurus;

internal sealed class ExportCommandSettings : CommandSettings
{
    [CommandOption("-c|--connection-string <CONNECTION_STRING>")]
    public string? ConnectionString { get; init; }

    [CommandOption("-p|--provider <PROVIDER>")]
    public string? Provider { get; init; }

    [CommandOption("-o|--output <FILE>")]
    public string? Output { get; init; }

    [CommandOption("--schema <SCHEMA>")]
    public string[]? Schemas { get; init; }

    [CommandOption("--table <TABLE>")]
    public string[]? Tables { get; init; }

    [CommandOption("--exclude-views")]
    public bool ExcludeViews { get; init; }

    [CommandOption("--exclude-stored-procedures")]
    public bool ExcludeStoredProcedures { get; init; }

    [CommandOption("--exclude-scalar-functions")]
    public bool ExcludeScalarFunctions { get; init; }

    [CommandOption("--exclude-table-valued-functions")]
    public bool ExcludeTableValuedFunctions { get; init; }

    [CommandOption("--exclude-sequences")]
    public bool ExcludeSequences { get; init; }

    [CommandOption("--exclude-user-defined-types")]
    public bool ExcludeUserDefinedTypes { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return ValidationResult.Error("The connection string is required.");

        if (string.IsNullOrWhiteSpace(Provider))
            return ValidationResult.Error("The provider is required.");

        if (Output is not null && string.IsNullOrWhiteSpace(Output))
            return ValidationResult.Error("The output file path cannot be blank.");

        return ValidationResult.Success();
    }
}
