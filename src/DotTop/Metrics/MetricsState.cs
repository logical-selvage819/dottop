namespace DotTop.Metrics;

public sealed class MetricsState
{
    public const int SparklineWidth = 60;

    public TimeSeries CpuUsage { get; } = new(SparklineWidth);
    public TimeSeries WorkingSetMb { get; } = new(SparklineWidth);
    public TimeSeries GcHeapMb { get; } = new(SparklineWidth);
    public TimeSeries AllocRateMb { get; } = new(SparklineWidth);
    public TimeSeries ThreadPoolQueueLength { get; } = new(SparklineWidth);
    public TimeSeries ThreadPoolThreadCount { get; } = new(SparklineWidth);
    public TimeSeries ExceptionsPerSec { get; } = new(SparklineWidth);
    public TimeSeries RequestsPerSec { get; } = new(SparklineWidth);
    public TimeSeries ActiveRequests { get; } = new(SparklineWidth);
    public TimeSeries TimeInGcPercent { get; } = new(SparklineWidth);

    public LatencyWindow Latency { get; } = new(2048);
    public EndpointTable Endpoints { get; } = new();

    public long TotalRequests;
    public long FailedRequests;
    public long ActiveRequestsNow;
    public long TotalExceptions;
    public long ActiveConnections;
    public long TotalConnections;
    public double Gen0Count;
    public double Gen1Count;
    public double Gen2Count;

    public DateTime LastUpdated = DateTime.UtcNow;
    public DateTime SessionStart = DateTime.UtcNow;
}
