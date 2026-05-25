using DotTop.Diagnostics;
using DotTop.Discovery;
using DotTop.Metrics;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DotTop.UI;

public sealed class Dashboard
{
    private readonly DotNetProcessInfo _process;
    private readonly MetricsAggregator _aggregator;
    private readonly DiagnosticsSession _session;
    private readonly DateTime _attachedAt = DateTime.Now;
    private string? _faultMessage;

    public Dashboard(DotNetProcessInfo process, DiagnosticsSession session, MetricsAggregator aggregator)
    {
        _process = process;
        _session = session;
        _aggregator = aggregator;
        _session.Faulted += ex =>
        {
            _faultMessage = ex.Message;
        };
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var layout = BuildLayout();
        _ = Task.Run(() => ListenForKeys(ct), ct);

        await AnsiConsole.Live(layout)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!ct.IsCancellationRequested)
                {
                    Update(layout);
                    ctx.Refresh();
                    try { await Task.Delay(TimeSpan.FromMilliseconds(500), ct); }
                    catch (OperationCanceledException) { break; }
                }
            });
    }

    private static Layout BuildLayout()
    {
        return new Layout("root")
            .SplitRows(
                new Layout("header").Size(3),
                new Layout("body").SplitColumns(
                    new Layout("left"),
                    new Layout("right")),
                new Layout("endpoints").Size(14),
                new Layout("footer").Size(1));
    }

    private void Update(Layout layout)
    {
        layout["header"].Update(BuildHeader());
        layout["left"].Update(BuildRuntimePanel());
        layout["right"].Update(BuildAspNetPanel());
        layout["endpoints"].Update(BuildEndpointsPanel());
        layout["footer"].Update(BuildFooter());
    }

    private IRenderable BuildHeader()
    {
        var s = _aggregator.State;
        var pidName = $"[aqua]{Escape(_process.Name)}[/] [grey]pid[/] [white]{_process.Pid}[/]";
        var runtime = $"[grey]runtime[/] [white]{Escape(_process.RuntimeVersion ?? "?")}[/]";
        var uptime = $"[grey]uptime[/] [white]{FormatUptime(DateTime.Now - _process.StartTime)}[/]";
        var attached = $"[grey]attached[/] [white]{FormatUptime(DateTime.Now - _attachedAt)}[/]";

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap().RightAligned())
            .AddRow(
                new Markup($"{pidName}   {runtime}   {uptime}   {attached}"),
                new Markup($"[grey]q[/] quit  [grey]r[/] reset endpoints"));

        var panel = new Panel(grid)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Header("[bold cyan1] dottop [/]")
            .Expand();
        return panel;
    }

    private IRenderable BuildRuntimePanel()
    {
        var s = _aggregator.State;
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().PadLeft(1));

        AddMetricRow(grid, "CPU",        $"{s.CpuUsage.Last,6:F1} %",   s.CpuUsage,        CpuColor(s.CpuUsage.Last), max: 100);
        AddMetricRow(grid, "Working set", $"{s.WorkingSetMb.Last,6:F0} MB", s.WorkingSetMb,  Color.Aqua);
        AddMetricRow(grid, "GC heap",     $"{s.GcHeapMb.Last,6:F0} MB",     s.GcHeapMb,     Color.Aqua);
        AddMetricRow(grid, "Allocs",      $"{s.AllocRateMb.Last,6:F2} MB/s", s.AllocRateMb, Color.Yellow);
        AddMetricRow(grid, "Time in GC",  $"{s.TimeInGcPercent.Last,6:F1} %", s.TimeInGcPercent, GcColor(s.TimeInGcPercent.Last), max: 100);
        AddMetricRow(grid, "Threads",     $"{(int)s.ThreadPoolThreadCount.Last,6}",  s.ThreadPoolThreadCount, Color.Green);
        AddMetricRow(grid, "TP queue",    $"{(int)s.ThreadPoolQueueLength.Last,6}",  s.ThreadPoolQueueLength, QueueColor((int)s.ThreadPoolQueueLength.Last));
        AddMetricRow(grid, "Exceptions",  $"{s.ExceptionsPerSec.Last,6:F1} /s",      s.ExceptionsPerSec, Color.Red);

        grid.AddRow(new Markup(""), new Markup(""));
        grid.AddRow(new Markup("[grey]GC counts[/]"), new Markup($"[white]g0[/] {(long)s.Gen0Count}  [white]g1[/] {(long)s.Gen1Count}  [white]g2[/] {(long)s.Gen2Count}"));

        return new Panel(grid)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Header("[bold] Runtime [/]")
            .Expand();
    }

    private IRenderable BuildAspNetPanel()
    {
        var s = _aggregator.State;
        var latency = s.Latency.Snapshot();

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().PadLeft(1));

        AddMetricRow(grid, "Req/sec",   $"{s.RequestsPerSec.Last,6:F1}",   s.RequestsPerSec, Color.Green);
        AddMetricRow(grid, "Active",    $"{s.ActiveRequestsNow,6}",        s.ActiveRequests, Color.Aqua);
        grid.AddRow(new Markup("[grey]Total requests[/]"), new Markup($"[white]{s.TotalRequests,12:N0}[/]"));
        grid.AddRow(new Markup("[grey]Failed[/]"),         new Markup($"[red]{s.FailedRequests,12:N0}[/]"));

        grid.AddRow(new Markup(""), new Markup(""));
        grid.AddRow(new Markup("[grey]Latency (window)[/]"), new Markup($"[grey]{latency.Count} samples[/]"));
        grid.AddRow(new Markup("  avg"), new Markup($"[white]{latency.Avg,8:F1} ms[/]"));
        grid.AddRow(new Markup("  p50"), new Markup($"[white]{latency.P50,8:F1} ms[/]"));
        grid.AddRow(new Markup("  p95"), new Markup($"[{LatencyColor(latency.P95)}]{latency.P95,8:F1} ms[/]"));
        grid.AddRow(new Markup("  p99"), new Markup($"[{LatencyColor(latency.P99)}]{latency.P99,8:F1} ms[/]"));

        grid.AddRow(new Markup(""), new Markup(""));
        grid.AddRow(new Markup("[grey]Connections[/]"), new Markup($"[white]{s.ActiveConnections,8:N0}[/] active   [grey]{s.TotalConnections:N0} total[/]"));

        return new Panel(grid)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Header("[bold] ASP.NET Core [/]")
            .Expand();
    }

    private IRenderable BuildEndpointsPanel()
    {
        var top = _aggregator.State.Endpoints.Top(12);

        var table = new Table()
            .NoBorder()
            .Expand()
            .AddColumn(new TableColumn("[grey]Method[/]"))
            .AddColumn(new TableColumn("[grey]Path[/]"))
            .AddColumn(new TableColumn("[grey]Count[/]").RightAligned())
            .AddColumn(new TableColumn("[grey]Avg ms[/]").RightAligned())
            .AddColumn(new TableColumn("[grey]Max ms[/]").RightAligned())
            .AddColumn(new TableColumn("[grey]5xx[/]").RightAligned());

        if (top.Count == 0)
        {
            table.AddRow(new Markup("[grey]—[/]"), new Markup("[grey]waiting for traffic[/]"), new Markup(""), new Markup(""), new Markup(""), new Markup(""));
        }
        else
        {
            foreach (var e in top)
            {
                table.AddRow(
                    new Markup($"[aqua]{Escape(e.Method)}[/]"),
                    new Markup($"[white]{Escape(Truncate(e.Path, 60))}[/]"),
                    new Markup($"[white]{e.Count,8:N0}[/]"),
                    new Markup($"[white]{e.AvgLatencyMs,8:F1}[/]"),
                    new Markup($"[{LatencyColor(e.MaxLatencyMs)}]{e.MaxLatencyMs,8:F1}[/]"),
                    new Markup(e.ErrorCount > 0 ? $"[red]{e.ErrorCount}[/]" : "[grey]0[/]"));
            }
        }

        return new Panel(table)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Header("[bold] Hottest endpoints [/]")
            .Expand();
    }

    private IRenderable BuildFooter()
    {
        if (_faultMessage is not null)
        {
            return new Markup($"[red] !! diagnostics fault: {Escape(_faultMessage)}[/]");
        }
        var s = _aggregator.State;
        var since = DateTime.UtcNow - s.LastUpdated;
        var fresh = since.TotalSeconds < 3 ? "[green]live[/]" : "[yellow]stale[/]";
        var dropped = _aggregator.DroppedRequestStops;
        return new Markup($" {fresh}  [grey]last counter[/] {since.TotalSeconds,4:F1}s ago  [grey]dropped stops[/] {dropped}   [grey]press q to quit[/]");
    }

    private void AddMetricRow(Grid grid, string label, string value, TimeSeries series, Color color, double? max = null)
    {
        var (vals, _, smax) = series.Snapshot();
        var hi = max ?? (smax <= 0 ? 1 : smax);
        var spark = Sparkline.Render(vals, hi);
        var line = $"[white]{value,-12}[/] [{color.ToMarkup()}]{spark}[/]";
        grid.AddRow(new Markup($"[grey]{label}[/]"), new Markup(line));
    }

    private static string LatencyColor(double ms) => ms switch
    {
        < 50 => "green",
        < 200 => "yellow",
        < 1000 => "orange1",
        _ => "red",
    };

    private static Color CpuColor(double v) => v switch
    {
        < 50 => Color.Green,
        < 80 => Color.Yellow,
        _ => Color.Red,
    };

    private static Color GcColor(double v) => v switch
    {
        < 10 => Color.Green,
        < 30 => Color.Yellow,
        _ => Color.Red,
    };

    private static Color QueueColor(int v) => v switch
    {
        0 => Color.Green,
        < 50 => Color.Yellow,
        _ => Color.Red,
    };

    private void ListenForKeys(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!Console.KeyAvailable) { Thread.Sleep(50); continue; }
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
                {
                    Program.RequestShutdown();
                    break;
                }
                if (key == ConsoleKey.R)
                {
                    _aggregator.State.Endpoints.Clear();
                }
            }
        }
        catch
        {
            // Console redirected — keyboard input not available.
        }
    }

    private static string FormatUptime(TimeSpan t)
    {
        if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d{t.Hours:D2}h{t.Minutes:D2}m";
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h{t.Minutes:D2}m";
        if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}m{t.Seconds:D2}s";
        return $"{(int)t.TotalSeconds}s";
    }

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max) return s;
        return s.Substring(0, max - 1) + "…";
    }

    private static string Escape(string s) => Markup.Escape(s);
}

internal static class ColorMarkupExt
{
    public static string ToMarkup(this Color c) => c.ToString().ToLowerInvariant();
}
