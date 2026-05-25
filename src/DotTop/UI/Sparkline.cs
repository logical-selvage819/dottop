namespace DotTop.UI;

internal static class Sparkline
{
    private static readonly char[] Blocks = { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };

    public static string Render(double[] values, double? max = null)
    {
        if (values.Length == 0) return new string(' ', 1);

        double hi = max ?? double.MinValue;
        if (!max.HasValue)
        {
            for (int i = 0; i < values.Length; i++) if (values[i] > hi) hi = values[i];
        }
        if (hi <= 0) hi = 1;

        Span<char> chars = stackalloc char[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            var v = values[i];
            if (v < 0) v = 0;
            var ratio = v / hi;
            if (ratio > 1) ratio = 1;
            var idx = (int)Math.Round(ratio * (Blocks.Length - 1));
            chars[i] = Blocks[idx];
        }
        return new string(chars);
    }
}
