using System.ComponentModel;
using System.Globalization;
using AngouriMath;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ScottPlot;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot a mathematical expression y=f(x) and return a PNG image.")]
    public static CallToolResult plot_expression_png(
        [Description("Expression in variable x, for example sin(x), x^2, or exp(-x^2).")]
        string expression,
        [Description("Minimum x value.")]
        double x_min,
        [Description("Maximum x value.")]
        double x_max,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Number of sampled points, from 2 to 20000.")]
        int points = 500,
        [Description("Optional chart title.")]
        string? title = null)
    {
        try
        {
            var normalized = NormalizeExpression(expression);
            var samples = SampleExpression(normalized, x_min, x_max, points);
            var plot = CreatePlot(width, height, title ?? $"y = {normalized}", "x", "y");
            var line = plot.Add.Scatter(samples.X, samples.Y);
            line.LegendText = normalized;
            plot.ShowLegend();

            return PngResult(plot, width, height);
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not plot expression.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot multiple mathematical expressions y=f(x) on one chart and return a PNG image.")]
    public static CallToolResult plot_multiple_expressions_png(
        [Description("Expressions in variable x.")]
        string[] expressions,
        [Description("Minimum x value.")]
        double x_min,
        [Description("Maximum x value.")]
        double x_max,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Number of sampled points per expression, from 2 to 20000.")]
        int points = 500,
        [Description("Optional chart title.")]
        string? title = null)
    {
        try
        {
            if (expressions.Length == 0)
                throw new McpException("At least one expression is required.");
            if (expressions.Length > 20)
                throw new McpException("At most 20 expressions can be plotted at once.");

            var plot = CreatePlot(width, height, title ?? "Multiple expressions", "x", "y");
            foreach (var expression in expressions)
            {
                var normalized = NormalizeExpression(expression);
                var samples = SampleExpression(normalized, x_min, x_max, points);
                var line = plot.Add.Scatter(samples.X, samples.Y);
                line.LegendText = normalized;
            }

            plot.ShowLegend();
            return PngResult(plot, width, height);
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not plot expressions.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot x/y points as a line chart and return a PNG image.")]
    public static CallToolResult plot_points_png(
        [Description("X values.")]
        double[] x_values,
        [Description("Y values.")]
        double[] y_values,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        ValidatePairedSeries(x_values, y_values);
        var plot = CreatePlot(width, height, title ?? "Points", "x", "y");
        plot.Add.Scatter(x_values, y_values);
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot x/y points as a scatter chart and return a PNG image.")]
    public static CallToolResult plot_scatter_png(
        [Description("X values.")]
        double[] x_values,
        [Description("Y values.")]
        double[] y_values,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        ValidatePairedSeries(x_values, y_values);
        var plot = CreatePlot(width, height, title ?? "Scatter", "x", "y");
        var scatter = plot.Add.Scatter(x_values, y_values);
        scatter.LineWidth = 0;
        scatter.MarkerSize = 6;
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot a bar chart from labels and values and return a PNG image.")]
    public static CallToolResult plot_bar_chart_png(
        [Description("Bar labels.")]
        string[] labels,
        [Description("Bar values.")]
        double[] values,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        if (labels.Length != values.Length)
            throw new McpException("labels and values must have the same length.");
        ValidateNumbers(values, nameof(values), requireNonEmpty: true);
        if (labels.Any(string.IsNullOrWhiteSpace))
            throw new McpException("labels cannot contain empty values.");

        var plot = CreatePlot(width, height, title ?? "Bar chart", "", "value");
        var positions = Enumerable.Range(0, values.Length).Select(i => (double)i).ToArray();
        plot.Add.Bars(positions, values);
        plot.Axes.Bottom.SetTicks(positions, labels);
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot a histogram from numeric values and return a PNG image.")]
    public static CallToolResult plot_histogram_png(
        [Description("Values to bin.")]
        double[] values,
        [Description("Number of bins, from 1 to 200.")]
        int bins = 20,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        ValidateNumbers(values, nameof(values), requireNonEmpty: true);
        if (bins is < 1 or > 200)
            throw new McpException("bins must be between 1 and 200.");

        var histogram = ScottPlot.Statistics.Histogram.WithBinCount(bins, values);
        var plot = CreatePlot(width, height, title ?? "Histogram", "value", "count");
        plot.Add.Bars(histogram.Bins, histogram.Counts);
        return PngResult(plot, width, height);
    }


    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot polar data from angles and radii and return a PNG image.")]
    public static CallToolResult plot_polar_png(
        [Description("Angle values.")]
        double[] angles,
        [Description("Radius values.")]
        double[] radii,
        [Description("Angle unit: degrees or radians.")]
        string angle_unit = "degrees",
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 900,
        [Description("Optional chart title.")]
        string? title = null)
    {
        ValidatePairedSeries(angles, radii);
        if (radii.Any(value => value < 0))
            throw new McpException("radii cannot contain negative values.");

        var radians = angles.Select(angle => ToRadians(angle, angle_unit)).ToArray();
        var xs = radii.Zip(radians, (radius, angle) => radius * Math.Cos(angle)).ToArray();
        var ys = radii.Zip(radians, (radius, angle) => radius * Math.Sin(angle)).ToArray();
        var maxRadius = Math.Max(radii.Max(), 1e-12);

        var plot = CreatePlot(width, height, title ?? "Polar plot", "", "");
        plot.Add.PolarAxis(maxRadius, maxRadius, circleCount: 4, spokeCount: 12);
        plot.Add.Scatter(xs, ys);
        plot.Axes.SquareUnits();
        plot.Axes.SetLimits(-maxRadius, maxRadius, -maxRadius, maxRadius);
        plot.HideGrid();
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot a heatmap from a rectangular matrix and return a PNG image.")]
    public static CallToolResult plot_heatmap_png(
        [Description("Rectangular matrix of values.")]
        double[][] values,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        var matrix = ToRectangularArray(values, nameof(values));
        var plot = CreatePlot(width, height, title ?? "Heatmap", "column", "row");
        plot.Add.Heatmap(matrix);
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot a time series from timestamps and values and return a PNG image.")]
    public static CallToolResult plot_time_series_png(
        [Description("Date/time values parseable by DateTimeOffset, or Unix timestamps in seconds/milliseconds.")]
        string[] timestamps,
        [Description("Y values.")]
        double[] values,
        [Description("Image width in pixels.")]
        int width = 1000,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        if (timestamps.Length != values.Length)
            throw new McpException("timestamps and values must have the same length.");
        ValidateNumbers(values, nameof(values), requireNonEmpty: true);
        var xs = timestamps.Select(ParseTimestampAsScottPlotNumber).ToArray();

        var plot = CreatePlot(width, height, title ?? "Time series", "time", "value");
        plot.Add.Scatter(xs, values);
        plot.Axes.DateTimeTicksBottom();
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot points with vertical error bars and return a PNG image.")]
    public static CallToolResult plot_error_bars_png(
        [Description("X values.")]
        double[] x_values,
        [Description("Y values.")]
        double[] y_values,
        [Description("Symmetric Y error values.")]
        double[] y_errors,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        ValidatePairedSeries(x_values, y_values);
        ValidateNumbers(y_errors, nameof(y_errors), requireNonEmpty: true);
        if (y_errors.Length != y_values.Length)
            throw new McpException("y_errors and y_values must have the same length.");
        if (y_errors.Any(value => value < 0))
            throw new McpException("y_errors cannot contain negative values.");

        var plot = CreatePlot(width, height, title ?? "Error bars", "x", "y");
        var scatter = plot.Add.Scatter(x_values, y_values);
        scatter.LineWidth = 0;
        scatter.MarkerSize = 6;
        plot.Add.ErrorBar(x_values, y_values, y_errors);
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot one or more box plots from grouped values and return a PNG image.")]
    public static CallToolResult plot_box_plot_png(
        [Description("Groups of numeric values. Each group becomes one box.")]
        double[][] groups,
        [Description("Optional labels for groups.")]
        string[]? labels = null,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        if (groups.Length == 0)
            throw new McpException("groups must contain at least one group.");
        if (labels is not null && labels.Length != groups.Length)
            throw new McpException("labels length must match groups length.");

        var boxes = new List<Box>();
        for (var i = 0; i < groups.Length; i++)
        {
            ValidateNumbers(groups[i], $"groups[{i}]", requireNonEmpty: true);
            boxes.Add(CreateBox(groups[i], i));
        }

        var plot = CreatePlot(width, height, title ?? "Box plot", "group", "value");
        plot.Add.Boxes(boxes);
        if (labels is not null)
            plot.Axes.Bottom.SetTicks(Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot x/y points as a step chart and return a PNG image.")]
    public static CallToolResult plot_step_png(
        [Description("X values.")]
        double[] x_values,
        [Description("Y values.")]
        double[] y_values,
        [Description("Step direction: horizontal or vertical.")]
        string step_direction = "horizontal",
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        ValidatePairedSeries(x_values, y_values);
        var plot = CreatePlot(width, height, title ?? "Step plot", "x", "y");
        var scatter = plot.Add.Scatter(x_values, y_values);
        scatter.ConnectStyle = NormalizeStepDirection(step_direction);
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot x/y points as an area chart and return a PNG image.")]
    public static CallToolResult plot_area_png(
        [Description("X values.")]
        double[] x_values,
        [Description("Y values.")]
        double[] y_values,
        [Description("Baseline Y value for the filled area.")]
        double baseline = 0,
        [Description("Image width in pixels.")]
        int width = 900,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        ValidatePairedSeries(x_values, y_values);
        ValidateFinite(baseline, nameof(baseline));
        var plot = CreatePlot(width, height, title ?? "Area chart", "x", "y");
        var scatter = plot.Add.Scatter(x_values, y_values);
        scatter.FillY = true;
        scatter.FillYValue = baseline;
        return PngResult(plot, width, height);
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Plot a pie chart from labels and values and return a PNG image.")]
    public static CallToolResult plot_pie_png(
        [Description("Slice labels.")]
        string[] labels,
        [Description("Slice values. Values must be non-negative and at least one value must be positive.")]
        double[] values,
        [Description("Image width in pixels.")]
        int width = 800,
        [Description("Image height in pixels.")]
        int height = 600,
        [Description("Optional chart title.")]
        string? title = null)
    {
        if (labels.Length != values.Length)
            throw new McpException("labels and values must have the same length.");
        ValidateNumbers(values, nameof(values), requireNonEmpty: true);
        if (values.Any(value => value < 0))
            throw new McpException("values cannot contain negative values.");
        if (values.All(value => value == 0))
            throw new McpException("at least one value must be positive.");
        if (labels.Any(string.IsNullOrWhiteSpace))
            throw new McpException("labels cannot contain empty values.");

        var plot = CreatePlot(width, height, title ?? "Pie chart", "", "");
        var pie = plot.Add.Pie(values);
        for (var i = 0; i < pie.Slices.Count; i++)
        {
            pie.Slices[i].LabelText = labels[i];
            pie.Slices[i].LegendText = labels[i];
        }
        plot.ShowLegend();
        plot.HideAxesAndGrid();
        return PngResult(plot, width, height);
    }
    private static (double[] X, double[] Y) SampleExpression(string expression, double xMin, double xMax, int points)
    {
        if (points < 2 || points > 20_000)
            throw new McpException("points must be between 2 and 20000.");
        if (!double.IsFinite(xMin) || !double.IsFinite(xMax))
            throw new McpException("x_min and x_max must be finite numbers.");
        if (xMax <= xMin)
            throw new McpException("x_max must be greater than x_min.");

        var expr = ParseEntity(expression);
        var x = MathS.Var("x");
        var step = (xMax - xMin) / (points - 1);
        var xs = new double[points];
        var ys = new double[points];

        for (var i = 0; i < points; i++)
        {
            var xValue = xMin + step * i;
            xs[i] = xValue;
            ys[i] = EvaluateReal(expr, x, xValue);
        }

        return (xs, ys);
    }

    private static Plot CreatePlot(int width, int height, string title, string xLabel, string yLabel)
    {
        ValidateImageSize(width, height);
        var plot = new Plot();
        plot.Title(title);
        plot.XLabel(xLabel);
        plot.YLabel(yLabel);
        return plot;
    }

    private static CallToolResult PngResult(Plot plot, int width, int height)
    {
        var image = plot.GetImage(width, height);
        var pngBytes = image.GetImageBytes(ImageFormat.Png);
        return new CallToolResult
        {
            Content = [ImageContentBlock.FromBytes(pngBytes, "image/png")]
        };
    }


    private static double ToRadians(double angle, string unit)
    {
        return NormalizeOperation(unit) switch
        {
            "degree" or "degrees" or "deg" => angle * Math.PI / 180,
            "radian" or "radians" or "rad" => angle,
            _ => throw new McpException("angle_unit must be degrees or radians.")
        };
    }

    private static double[,] ToRectangularArray(double[][] values, string name)
    {
        if (values.Length == 0)
            throw new McpException($"{name} must contain at least one row.");
        if (values.Any(row => row.Length == 0))
            throw new McpException($"{name} rows cannot be empty.");

        var rowCount = values.Length;
        var columnCount = values[0].Length;
        var result = new double[rowCount, columnCount];

        for (var row = 0; row < rowCount; row++)
        {
            if (values[row].Length != columnCount)
                throw new McpException($"{name} must be rectangular.");
            ValidateNumbers(values[row], $"{name}[{row}]", requireNonEmpty: true);

            for (var column = 0; column < columnCount; column++)
                result[row, column] = values[row][column];
        }

        return result;
    }

    private static double ParseTimestampAsScottPlotNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new McpException("timestamps cannot contain empty values.");

        var trimmed = value.Trim();
        DateTimeOffset dto;
        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
        {
            dto = Math.Abs(unix) >= 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
                : DateTimeOffset.FromUnixTimeSeconds(unix);
        }
        else if (!DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dto))
        {
            throw new McpException($"Could not parse timestamp '{value}'.");
        }

        return NumericConversion.ToNumber(dto.UtcDateTime);
    }

    private static Box CreateBox(double[] values, double position)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        return new Box
        {
            Position = position,
            BoxMin = PercentileSorted(sorted, 25),
            BoxMiddle = Median(sorted),
            BoxMax = PercentileSorted(sorted, 75),
            WhiskerMin = sorted[0],
            WhiskerMax = sorted[^1]
        };
    }

    private static ConnectStyle NormalizeStepDirection(string stepDirection)
    {
        return NormalizeOperation(stepDirection) switch
        {
            "horizontal" or "h" => ConnectStyle.StepHorizontal,
            "vertical" or "v" => ConnectStyle.StepVertical,
            _ => throw new McpException("step_direction must be horizontal or vertical.")
        };
    }
    private static void ValidatePairedSeries(double[] xValues, double[] yValues)
    {
        ValidateNumbers(xValues, nameof(xValues), requireNonEmpty: true);
        ValidateNumbers(yValues, nameof(yValues), requireNonEmpty: true);
        if (xValues.Length != yValues.Length)
            throw new McpException("x_values and y_values must have the same length.");
    }

    private static void ValidateImageSize(int width, int height)
    {
        if (width is < 200 or > 4000)
            throw new McpException("width must be between 200 and 4000 pixels.");
        if (height is < 200 or > 4000)
            throw new McpException("height must be between 200 and 4000 pixels.");
    }
}



