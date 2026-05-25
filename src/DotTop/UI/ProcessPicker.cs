using DotTop.Discovery;
using Spectre.Console;

namespace DotTop.UI;

public static class ProcessPicker
{
    public static DotNetProcessInfo? Pick(IReadOnlyList<DotNetProcessInfo> processes)
    {
        if (processes.Count == 0) return null;

        AnsiConsole.Write(new FigletText("dottop").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[grey]htop for ASP.NET Core and .NET applications[/]");
        AnsiConsole.WriteLine();

        if (processes.Count == 1)
        {
            var only = processes[0];
            AnsiConsole.MarkupLine($"[grey]Only one .NET process found — attaching to[/] [aqua]{Escape(only.Name)}[/] [grey](pid {only.Pid})[/]");
            return only;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]Discovered .NET processes[/]")
            .AddColumns(
                new TableColumn("[grey]PID[/]").RightAligned(),
                new TableColumn("[grey]Name[/]"),
                new TableColumn("[grey]Runtime[/]"),
                new TableColumn("[grey]Uptime[/]").RightAligned(),
                new TableColumn("[grey]Diagnostics[/]"));

        foreach (var p in processes)
        {
            table.AddRow(
                new Markup($"[white]{p.Pid}[/]"),
                new Markup($"[aqua]{Escape(p.Name)}[/]"),
                new Markup($"[grey]{Escape(p.RuntimeVersion ?? "?")}[/]"),
                new Markup($"[grey]{FormatUptime(p.Uptime)}[/]"),
                new Markup(p.DiagnosticsReachable ? "[green]ready[/]" : "[red]locked[/]"));
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var prompt = new SelectionPrompt<DotNetProcessInfo>()
            .Title("Select a process to attach to:")
            .PageSize(Math.Max(3, Math.Min(15, processes.Count)))
            .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
            .UseConverter(p => $"{p.Pid,7}  {p.Name,-30}  {p.RuntimeVersion ?? "?",-12}  {(p.DiagnosticsReachable ? "ready" : "locked")}")
            .AddChoices(processes);

        return AnsiConsole.Prompt(prompt);
    }

    private static string FormatUptime(TimeSpan t)
    {
        if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d{t.Hours:D2}h";
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h{t.Minutes:D2}m";
        if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}m{t.Seconds:D2}s";
        return $"{(int)t.TotalSeconds}s";
    }

    private static string Escape(string s) => Markup.Escape(s);
}
