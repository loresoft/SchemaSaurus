using Spectre.Console.Cli;

namespace SchemaSaurus;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(configurator =>
        {
            var exportCommand = configurator.AddCommand<ExportCommand>("export");
            exportCommand.WithDescription("Export database schema metadata as JSON.");
        });

        return app.RunAsync(args);
    }
}
