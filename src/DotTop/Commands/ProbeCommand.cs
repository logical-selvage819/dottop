using System.ComponentModel;
using DotTop.Diagnostics;
using DotTop.Discovery;
using DotTop.Metrics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotTop.Commands;

public sealed class ProbeCommand : AsyncCommand<ProbeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Process id or (partial) name to probe")]
        [CommandArgument(0, "<target>")]
        public string Target { get; init; } = "";

        [Description("How many seconds to listen before exiting")]
        [CommandOption("-s|--seconds <SECONDS>")]
        [DefaultValue(5)]
        public int Seconds { get; init; } = 5;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        DotNetProcessInfo? target;
        if (int.TryParse(settings.Target, out var pid))
        {
            target = ProcessDiscovery.FindByPid(pid);
        }
        else
        {
            var matches = ProcessDiscovery.FindByName(settings.Target);
            target = matches.FirstOrDefault();
        }
        if (target is null)
        {
            AnsiConsole.MarkupLine("[red]no matching .NET process[/]");
            return 2;
        }

        await using var session = new DiagnosticsSession(target.Pid, 1);
        var agg = new MetricsAggregator();
        agg.Attach(session);

        var counterNames = new HashSet<string>();
        session.CounterReceived += s => counterNames.Add($"{s.Provider}/{s.Name}");

        var requestCount = 0;
        session.RequestStopped += _ => Interlocked.Increment(ref requestCount);

        session.Faulted += ex => AnsiConsole.MarkupLine($"[red]fault: {Markup.Escape(ex.Message)}[/]");

        session.Start();
        AnsiConsole.MarkupLine($"[grey]listening to pid {target.Pid} ({Markup.Escape(target.Name)}) for {settings.Seconds}s…[/]");

        try { await Task.Delay(TimeSpan.FromSeconds(settings.Seconds), ct); }
        catch (OperationCanceledException) { }

        var s = agg.State;
        AnsiConsole.MarkupLine($"[bold]counters captured:[/] {counterNames.Count}");
        foreach (var name in counterNames.OrderBy(x => x))
        {
            AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(name)}[/]");
        }
        AnsiConsole.MarkupLine($"[bold]requests:[/] started={s.TotalRequests} stopped={requestCount} active={s.ActiveRequestsNow} dropped-stops={agg.DroppedRequestStops}");
        var lat = s.Latency.Snapshot();
        AnsiConsole.MarkupLine($"[bold]latency samples:[/] {lat.Count}  avg={lat.Avg:F1}ms p95={lat.P95:F1}ms p99={lat.P99:F1}ms");
        var top = s.Endpoints.Top(8);
        AnsiConsole.MarkupLine($"[bold]top endpoints:[/]");
        foreach (var e in top)
        {
            AnsiConsole.MarkupLine($"  [aqua]{e.Method}[/] [white]{Markup.Escape(e.Path)}[/]  n={e.Count} avg={e.AvgLatencyMs:F1}ms max={e.MaxLatencyMs:F1}ms");
        }
        AnsiConsole.MarkupLine($"[bold]runtime:[/] cpu={s.CpuUsage.Last:F1}% ws={s.WorkingSetMb.Last:F0}MB heap={s.GcHeapMb.Last:F0}MB alloc={s.AllocRateMb.Last:F2}MB/s tp={(int)s.ThreadPoolThreadCount.Last} queue={(int)s.ThreadPoolQueueLength.Last}");
        return 0;
    }
}
