using DotTop.Commands;
using DotTop.Diagnostics;
using DotTop.Discovery;
using DotTop.Metrics;
using DotTop.UI;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotTop;

public static class Program
{
    private static CancellationTokenSource? s_shutdown;

    public static int Main(string[] args)
    {
        var app = new CommandApp<DefaultCommand>();
        app.Configure(cfg =>
        {
            cfg.SetApplicationName("dottop");
            cfg.AddCommand<AttachCommand>("attach")
                .WithDescription("Attach to a running .NET process by PID or name")
                .WithExample("attach", "4211")
                .WithExample("attach", "payment-api");
            cfg.AddCommand<ProbeCommand>("probe")
                .WithDescription("Attach briefly, print captured counters/requests, and exit (non-interactive)")
                .WithExample("probe", "payment-api", "--seconds", "5");
        });
        return app.Run(args);
    }

    public static void RequestShutdown() => s_shutdown?.Cancel();

    public static async Task<int> AttachAndRun(DotNetProcessInfo target, int intervalSeconds)
    {
        if (!target.DiagnosticsReachable)
        {
            AnsiConsole.MarkupLine($"[red]Process {target.Pid} ({Markup.Escape(target.Name)}) is not reachable over the diagnostics port.[/]");
            AnsiConsole.MarkupLine("[grey]This usually means it runs under a different user, is single-file published without diagnostics, or has the port disabled.[/]");
            return 3;
        }

        AnsiConsole.Clear();

        await using var session = new DiagnosticsSession(target.Pid, intervalSeconds);
        var aggregator = new MetricsAggregator();
        aggregator.Attach(session);

        try
        {
            session.Start();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to start EventPipe session: {Markup.Escape(ex.Message)}[/]");
            return 4;
        }

        s_shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; s_shutdown?.Cancel(); };

        var dashboard = new Dashboard(target, session, aggregator);
        try
        {
            await dashboard.RunAsync(s_shutdown.Token);
        }
        catch (OperationCanceledException)
        {
        }

        AnsiConsole.MarkupLine("\n[grey]detached.[/]");
        return 0;
    }
}
