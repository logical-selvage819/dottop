using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace DotTop.Diagnostics;

public sealed class DiagnosticsSession : IAsyncDisposable
{
    private readonly int _pid;
    private readonly int _counterIntervalSec;
    private EventPipeSession? _session;
    private EventPipeEventSource? _source;
    private Task? _pumpTask;
    private CancellationTokenSource? _cts;

    public event Action<CounterSample>? CounterReceived;
    public event Action<RequestStarted>? RequestStarted;
    public event Action<RequestStopped>? RequestStopped;
    public event Action<ExceptionEvent>? ExceptionThrown;
    public event Action<GcCollectionEvent>? GcCollection;
    public event Action<Exception>? Faulted;

    public DiagnosticsSession(int pid, int counterIntervalSec = 1)
    {
        _pid = pid;
        _counterIntervalSec = counterIntervalSec;
    }

    public void Start()
    {
        if (_session is not null) throw new InvalidOperationException("Already started.");

        var client = new DiagnosticsClient(_pid);
        var providers = EventPipeProviders.Build(_counterIntervalSec);

        _session = client.StartEventPipeSession(providers, requestRundown: false, circularBufferMB: 64);
        _source = new EventPipeEventSource(_session.EventStream);

        Subscribe(_source);

        _cts = new CancellationTokenSource();
        _pumpTask = Task.Factory.StartNew(
            () =>
            {
                try { _source.Process(); }
                catch (Exception ex) when (!_cts.IsCancellationRequested) { Faulted?.Invoke(ex); }
            },
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private void Subscribe(EventPipeEventSource source)
    {
        source.Dynamic.All += OnDynamic;

        source.Clr.ExceptionStart += data =>
        {
            ExceptionThrown?.Invoke(new ExceptionEvent(
                data.ExceptionType ?? "Exception",
                data.ExceptionMessage,
                data.TimeStamp));
        };

        source.Clr.GCStart += data =>
        {
            GcCollection?.Invoke(new GcCollectionEvent(
                data.Depth,
                data.Reason.ToString(),
                data.TimeStamp));
        };
    }

    private void OnDynamic(TraceEvent evt)
    {
        if (evt.EventName == "EventCounters")
        {
            HandleCounter(evt);
            return;
        }

        if (evt.ProviderName == "Microsoft.AspNetCore.Hosting")
        {
            HandleHostingEvent(evt);
        }
    }

    private void HandleCounter(TraceEvent evt)
    {
        try
        {
            var payload = evt.PayloadValue(0) as IDictionary<string, object>;
            if (payload is null) return;
            if (!payload.TryGetValue("Payload", out var inner) || inner is not IDictionary<string, object> body) return;

            var name = body.TryGetValue("Name", out var n) ? n?.ToString() : null;
            if (string.IsNullOrEmpty(name)) return;

            var display = body.TryGetValue("DisplayName", out var d) ? d?.ToString() : null;
            var unit = body.TryGetValue("DisplayUnits", out var u) ? u?.ToString() : null;
            var counterType = body.TryGetValue("CounterType", out var ct) ? ct?.ToString() : null;
            var isIncrement = string.Equals(counterType, "Sum", StringComparison.OrdinalIgnoreCase);

            double value;
            if (isIncrement)
            {
                value = body.TryGetValue("Increment", out var inc) ? Convert.ToDouble(inc, System.Globalization.CultureInfo.InvariantCulture) : 0d;
            }
            else
            {
                value = body.TryGetValue("Mean", out var mean) ? Convert.ToDouble(mean, System.Globalization.CultureInfo.InvariantCulture) : 0d;
            }

            CounterReceived?.Invoke(new CounterSample(
                evt.ProviderName,
                name,
                display,
                unit,
                value,
                isIncrement,
                evt.TimeStamp));
        }
        catch
        {
            // Defensive — malformed counter payloads should never break the pump.
        }
    }

    private void HandleHostingEvent(TraceEvent evt)
    {
        try
        {
            switch (evt.EventName)
            {
                case "RequestStart":
                case "Request/Start":
                {
                    string method = SafePayloadString(evt, 0) ?? "?";
                    string path = SafePayloadString(evt, 1) ?? "?";
                    RequestStarted?.Invoke(new RequestStarted(evt.ActivityID, method, path, evt.TimeStamp));
                    break;
                }
                case "RequestStop":
                case "Request/Stop":
                {
                    int status = SafePayloadInt(evt, 0) ?? 0;
                    RequestStopped?.Invoke(new RequestStopped(evt.ActivityID, status, evt.TimeStamp));
                    break;
                }
                case "UnhandledException":
                {
                    ExceptionThrown?.Invoke(new ExceptionEvent("UnhandledException", null, evt.TimeStamp));
                    break;
                }
            }
        }
        catch
        {
            // Ignore malformed hosting events.
        }
    }

    private static string? SafePayloadString(TraceEvent evt, int idx)
    {
        if (idx >= evt.PayloadNames.Length) return null;
        return evt.PayloadValue(idx)?.ToString();
    }

    private static int? SafePayloadInt(TraceEvent evt, int idx)
    {
        if (idx >= evt.PayloadNames.Length) return null;
        var v = evt.PayloadValue(idx);
        return v is null ? null : Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async ValueTask DisposeAsync()
    {
        try { _cts?.Cancel(); } catch { }
        try { _session?.Stop(); } catch { }
        if (_pumpTask is not null)
        {
            try { await _pumpTask.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch { }
        }
        try { _source?.Dispose(); } catch { }
        try { _session?.Dispose(); } catch { }
        _cts?.Dispose();
    }
}
