using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute descriptive statistics for numeric data.")]
    public static object statistical_analysis(
        [Description("Numeric data points.")]
        double[] data)
    {
        if (data.Length == 0)
            throw new McpException("Data must contain at least one value.");
        if (data.Any(value => !double.IsFinite(value)))
            throw new McpException("All data values must be finite.");

        var sorted = data.OrderBy(x => x).ToArray();
        var count = data.Length;
        var sum = data.Sum();
        var mean = sum / count;
        var populationVariance = data.Select(x => Square(x - mean)).Sum() / count;
        var sampleVariance = count > 1 ? data.Select(x => Square(x - mean)).Sum() / (count - 1) : 0;
        var modes = data
            .GroupBy(x => x)
            .GroupBy(g => g.Count())
            .OrderByDescending(g => g.Key)
            .First()
            .Select(g => g.Key)
            .OrderBy(x => x)
            .ToArray();

        return new StatisticsResult(
            count,
            sum,
            mean,
            Median(sorted),
            modes,
            sorted[0],
            sorted[^1],
            populationVariance,
            Math.Sqrt(populationVariance),
            sampleVariance,
            Math.Sqrt(sampleVariance),
            PercentileSorted(sorted, 25),
            PercentileSorted(sorted, 75));
    }

    private static double Median(double[] sorted)
    {
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
    }

    private static double PercentileSorted(double[] sorted, double percentile)
    {
        if (sorted.Length == 1)
            return sorted[0];

        var position = (sorted.Length - 1) * percentile / 100;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        var weight = position - lower;

        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }
}
