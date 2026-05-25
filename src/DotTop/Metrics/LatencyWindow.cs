namespace DotTop.Metrics;

public sealed class LatencyWindow
{
    private readonly double[] _samples;
    private readonly object _lock = new();
    private int _head;
    private int _count;

    public LatencyWindow(int capacity = 1024)
    {
        _samples = new double[capacity];
    }

    public void Add(double latencyMs)
    {
        lock (_lock)
        {
            _samples[_head] = latencyMs;
            _head = (_head + 1) % _samples.Length;
            if (_count < _samples.Length) _count++;
        }
    }

    public LatencyStats Snapshot()
    {
        double[] copy;
        lock (_lock)
        {
            if (_count == 0) return new LatencyStats(0, 0, 0, 0, 0);
            copy = new double[_count];
            int start = _count < _samples.Length ? 0 : _head;
            for (int i = 0; i < _count; i++) copy[i] = _samples[(start + i) % _samples.Length];
        }

        Array.Sort(copy);
        var n = copy.Length;
        double sum = 0;
        for (int i = 0; i < n; i++) sum += copy[i];

        return new LatencyStats(
            n,
            Avg: sum / n,
            P50: Percentile(copy, 0.50),
            P95: Percentile(copy, 0.95),
            P99: Percentile(copy, 0.99));
    }

    private static double Percentile(double[] sortedAsc, double p)
    {
        if (sortedAsc.Length == 0) return 0;
        var idx = (int)Math.Ceiling(p * sortedAsc.Length) - 1;
        if (idx < 0) idx = 0;
        if (idx >= sortedAsc.Length) idx = sortedAsc.Length - 1;
        return sortedAsc[idx];
    }
}

public readonly record struct LatencyStats(int Count, double Avg, double P50, double P95, double P99);
