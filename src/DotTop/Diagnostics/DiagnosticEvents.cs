namespace DotTop.Diagnostics;

public readonly record struct CounterSample(
    string Provider,
    string Name,
    string? DisplayName,
    string? Unit,
    double Value,
    bool IsIncrement,
    DateTime Timestamp);

public readonly record struct RequestStarted(
    Guid ActivityId,
    string Method,
    string Path,
    DateTime Timestamp);

public readonly record struct RequestStopped(
    Guid ActivityId,
    int StatusCode,
    DateTime Timestamp);

public readonly record struct ExceptionEvent(
    string ExceptionType,
    string? Message,
    DateTime Timestamp);

public readonly record struct GcCollectionEvent(
    int Generation,
    string Reason,
    DateTime Timestamp);
