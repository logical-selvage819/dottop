using System.ComponentModel;
using DotTop.Discovery;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotTop.Commands;

public sealed class AttachCommand : AsyncCommand<AttachCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Process id or (partial) process name")]
        [CommandArgument(0, "<target>")]
        public string Target { get; init; } = "";

        [Description("Counter sampling interval in seconds")]
        [CommandOption("-i|--interval <SECONDS>")]
        [DefaultValue(1)]
        public int IntervalSeconds { get; init; } = 1;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        DotNetProcessInfo? target;
        if (int.TryParse(settings.Target, out var pid))
        {
            target = ProcessDiscovery.FindByPid(pid);
            if (target is null)
            {
                AnsiConsole.MarkupLine($"[red]No .NET process with pid {pid} is reachable.[/]");
                return 2;
            }
        }
        else
        {
            var matches = ProcessDiscovery.FindByName(settings.Target);
            if (matches.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]No .NET process matched name[/] [aqua]{Markup.Escape(settings.Target)}[/].");
                return 2;
            }
            if (matches.Count == 1)
            {
                target = matches[0];
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Multiple processes matched[/] [aqua]{Markup.Escape(settings.Target)}[/] [yellow]— pick one:[/]");
                target = DotTop.UI.ProcessPicker.Pick(matches);
                if (target is null) return 130;
            }
        }

        return await Program.AttachAndRun(target, settings.IntervalSeconds);
    }
}
