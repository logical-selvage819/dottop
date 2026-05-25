namespace DotTop.Metrics;

public sealed class TimeSeries
{
    private readonly double[] _buffer;
    private readonly object _lock = new();
    private int _head;
    private int _count;
    private double _last;

    public int Capacity => _buffer.Length;

    public TimeSeries(int capacity)
    {
        if (capacity < 2) capacity = 2;
        _buffer = new double[capacity];
    }

    public void Push(double value)
    {
        lock (_lock)
        {
            _buffer[_head] = value;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
            _last = value;
        }
    }

    public double Last { get { lock (_lock) return _last; } }

    public (double[] values, double min, double max) Snapshot()
    {
        lock (_lock)
        {
            var values = new double[_count];
            int start = _count < _buffer.Length ? 0 : _head;
            for (int i = 0; i < _count; i++)
            {
                values[i] = _buffer[(start + i) % _buffer.Length];
            }
            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < min) min = values[i];
                if (values[i] > max) max = values[i];
            }
            if (_count == 0) { min = 0; max = 0; }
            return (values, min, max);
        }
    }
}
