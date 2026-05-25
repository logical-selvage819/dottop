using System.Collections.Concurrent;
using DotTop.Diagnostics;

namespace DotTop.Metrics;

public sealed class MetricsAggregator
{
    private readonly ConcurrentDictionary<Guid, InFlight> _inFlight = new();

    public MetricsState State { get; } = new();

    public long DroppedRequestStops;

    public void Attach(DiagnosticsSession session)
    {
        session.CounterReceived += OnCounter;
        session.RequestStarted += OnRequestStarted;
        session.RequestStopped += OnRequestStopped;
        session.ExceptionThrown += OnException;
    }

    private void OnCounter(CounterSample s)
    {
        State.LastUpdated = DateTime.UtcNow;

        if (s.Provider == "System.Runtime")
        {
            switch (s.Name)
            {
                case "cpu-usage": State.CpuUsage.Push(s.Value); break;
                case "working-set": State.WorkingSetMb.Push(s.Value); break;
                case "gc-heap-size": State.GcHeapMb.Push(s.Value); break;
                case "alloc-rate": State.AllocRateMb.Push(s.Value / (1024 * 1024)); break;
                case "threadpool-queue-length": State.ThreadPoolQueueLength.Push(s.Value); break;
                case "threadpool-thread-count": State.ThreadPoolThreadCount.Push(s.Value); break;
                case "exception-count": State.ExceptionsPerSec.Push(s.Value); break;
                case "time-in-gc": State.TimeInGcPercent.Push(s.Value); break;
                case "gen-0-gc-count": State.Gen0Count = s.Value; break;
                case "gen-1-gc-count": State.Gen1Count = s.Value; break;
                case "gen-2-gc-count": State.Gen2Count = s.Value; break;
            }
            return;
        }

        if (s.Provider == "Microsoft.AspNetCore.Hosting")
        {
            switch (s.Name)
            {
                case "requests-per-second":
                case "request-rate":
                    State.RequestsPerSec.Push(s.Value);
                    break;
                case "total-requests": State.TotalRequests = (long)s.Value; break;
                case "current-requests":
                    State.ActiveRequestsNow = (long)s.Value;
                    State.ActiveRequests.Push(s.Value);
                    break;
                case "failed-requests": State.FailedRequests = (long)s.Value; break;
            }
            return;
        }

        if (s.Provider == "Microsoft-AspNetCore-Server-Kestrel")
        {
            switch (s.Name)
            {
                case "current-connections": State.ActiveConnections = (long)s.Value; break;
                case "total-connections": State.TotalConnections = (long)s.Value; break;
            }
        }
    }

    private void OnRequestStarted(RequestStarted e)
    {
        if (e.ActivityId == Guid.Empty) return;
        _inFlight[e.ActivityId] = new InFlight(e.Method, e.Path, e.Timestamp);
    }

    private void OnRequestStopped(RequestStopped e)
    {
        if (!_inFlight.TryRemove(e.ActivityId, out var started))
        {
            Interlocked.Increment(ref DroppedRequestStops);
            return;
        }
        var latencyMs = (e.Timestamp - started.StartTime).TotalMilliseconds;
        if (latencyMs < 0) latencyMs = 0;
        State.Latency.Add(latencyMs);
        State.Endpoints.Record(started.Method, started.Path, latencyMs, e.StatusCode);
    }

    private void OnException(ExceptionEvent e)
    {
        Interlocked.Increment(ref State.TotalExceptions);
    }

    private readonly record struct InFlight(string Method, string Path, DateTime StartTime);
}
