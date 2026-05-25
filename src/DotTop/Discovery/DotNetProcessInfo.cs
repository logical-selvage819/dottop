using System.Diagnostics;

namespace DotTop.Discovery;

public sealed record DotNetProcessInfo(
    int Pid,
    string Name,
    string? CommandLine,
    string? RuntimeVersion,
    string? OperatingSystem,
    DateTime StartTime,
    bool DiagnosticsReachable)
{
    public TimeSpan Uptime => DateTime.Now - StartTime;

    public string DisplayLine
    {
        get
        {
            var runtime = RuntimeVersion is null ? "?" : RuntimeVersion;
            var up = FormatUptime(Uptime);
            return $"[white]{Pid,7}[/]  [aqua]{Markup(Name),-28}[/]  [grey]{runtime,-12}[/]  [grey]{up,-10}[/]  {(DiagnosticsReachable ? "[green]ready[/]" : "[red]locked[/]")}";
        }
    }

    private static string FormatUptime(TimeSpan t)
    {
        if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d{t.Hours:D2}h";
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h{t.Minutes:D2}m";
        if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}m{t.Seconds:D2}s";
        return $"{(int)t.TotalSeconds}s";
    }

    private static string Markup(string s) => s.Replace("[", "[[").Replace("]", "]]");
}
