using System.Collections.Concurrent;

namespace DotTop.Metrics;

public sealed class EndpointTable
{
    private readonly ConcurrentDictionary<string, EndpointStats> _stats = new();

    public void Record(string method, string path, double latencyMs, int statusCode)
    {
        var key = $"{method} {path}";
        _stats.AddOrUpdate(
            key,
            _ => new EndpointStats { Method = method, Path = path, Count = 1, TotalLatencyMs = latencyMs, MaxLatencyMs = latencyMs, ErrorCount = statusCode >= 500 ? 1 : 0 },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Count++;
                    existing.TotalLatencyMs += latencyMs;
                    if (latencyMs > existing.MaxLatencyMs) existing.MaxLatencyMs = latencyMs;
                    if (statusCode >= 500) existing.ErrorCount++;
                }
                return existing;
            });
    }

    public IReadOnlyList<EndpointStats> Top(int n)
    {
        var snapshot = _stats.Values.Select(v =>
        {
            lock (v)
            {
                return new EndpointStats
                {
                    Method = v.Method,
                    Path = v.Path,
                    Count = v.Count,
                    TotalLatencyMs = v.TotalLatencyMs,
                    MaxLatencyMs = v.MaxLatencyMs,
                    ErrorCount = v.ErrorCount,
                };
            }
        });

        return snapshot
            .OrderByDescending(e => e.Count)
            .Take(n)
            .ToList();
    }

    public void Clear() => _stats.Clear();
}

public sealed class EndpointStats
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public long Count { get; set; }
    public double TotalLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public long ErrorCount { get; set; }
    public double AvgLatencyMs => Count == 0 ? 0 : TotalLatencyMs / Count;
}
