using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace DotTop.Diagnostics;

internal static class EventPipeProviders
{
    private const string CounterIntervalKey = "EventCounterIntervalSec";

    public static List<EventPipeProvider> Build(int counterIntervalSeconds)
    {
        var interval = new Dictionary<string, string>
        {
            [CounterIntervalKey] = counterIntervalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        return new List<EventPipeProvider>
        {
            // Activity ID propagation — without this, all events arrive with an empty
            // ActivityID and start/stop correlation is impossible.
            new(
                name: "System.Threading.Tasks.TplEventSource",
                eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                keywords: 0x80),

            new(
                name: "System.Runtime",
                eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                keywords: (long)ClrTraceEventParser.Keywords.None,
                arguments: interval),

            new(
                name: "Microsoft-Windows-DotNETRuntime",
                eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                keywords: (long)(ClrTraceEventParser.Keywords.Exception | ClrTraceEventParser.Keywords.GC)),

            new(
                name: "Microsoft.AspNetCore.Hosting",
                eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                keywords: 0xFFFFFFFFL,
                arguments: interval),

            new(
                name: "Microsoft-AspNetCore-Server-Kestrel",
                eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                keywords: 0xFFFFFFFFL,
                arguments: interval),

            new(
                name: "Microsoft.AspNetCore.Http.Connections",
                eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                keywords: 0xFFFFFFFFL,
                arguments: interval),
        };
    }
}
