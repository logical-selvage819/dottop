using System.ComponentModel;
using DotTop.Discovery;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotTop.Commands;

public sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Counter sampling interval in seconds")]
        [CommandOption("-i|--interval <SECONDS>")]
        [DefaultValue(1)]
        public int IntervalSeconds { get; init; } = 1;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var processes = ProcessDiscovery.Enumerate();
        if (processes.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No running .NET processes were found.[/]");
            AnsiConsole.MarkupLine("[grey]Tip: make sure the target process runs under the same user as dottop.[/]");
            return 1;
        }

        var picked = UI.ProcessPicker.Pick(processes);
        if (picked is null) return 130;

        return await Program.AttachAndRun(picked, settings.IntervalSeconds);
    }
}
