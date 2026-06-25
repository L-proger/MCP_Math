using System.Globalization;
using System.Numerics;
using AngouriMath;
using AngouriMath.Core;
using ModelContextProtocol;

namespace MCP_Math;

public sealed partial class MathTools
{
    private static Entity ParseEntity(string expression)
    {
        return MathS.FromString(NormalizeExpression(expression));
    }

    private static string NormalizeExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new McpException("Expression cannot be empty.");

        return expression.Trim().Replace("**", "^", StringComparison.Ordinal);
    }

    private static string NormalizeEquation(string equation)
    {
        var normalized = NormalizeExpression(equation).Replace("==", "=", StringComparison.Ordinal);
        var equalsAt = normalized.IndexOf('=', StringComparison.Ordinal);

        if (equalsAt < 0)
            return normalized;

        var left = normalized[..equalsAt].Trim();
        var right = normalized[(equalsAt + 1)..].Trim();

        if (left.Length == 0 || right.Length == 0)
            throw new McpException("Equation must contain expressions on both sides of '='.");

        return $"({left}) - ({right})";
    }

    private static McpException ToolError(string message, Exception ex)
    {
        return new McpException($"{message} {ex.Message}", ex);
    }

    private static ApproachFrom ParseApproach(string direction)
    {
        return NormalizeOperation(direction) switch
        {
            "both" or "bothsides" => ApproachFrom.BothSides,
            "left" => ApproachFrom.Left,
            "right" => ApproachFrom.Right,
            _ => throw new McpException("Direction must be both, left, or right.")
        };
    }

    private static string NormalizeOperation(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new McpException("Operation cannot be empty.");

        return operation.Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
    }

    private static double EvaluateReal(Entity expression, Entity.Variable variable, double value)
    {
        var number = expression.Substitute(variable, MathS.Numbers.Create(value)).EvalNumerical().ToNumerics();
        if (Math.Abs(number.Imaginary) > 1e-10)
            throw new McpException($"Function evaluated to a complex value at x = {value.ToString(CultureInfo.InvariantCulture)}.");

        return number.Real;
    }

    private static ComplexNumber FormatComplexValue(Complex value)
    {
        var real = CleanZero(value.Real);
        var imaginary = CleanZero(value.Imaginary);
        var text = imaginary == 0
            ? real.ToString("G17", CultureInfo.InvariantCulture)
            : real == 0
                ? $"{imaginary.ToString("G17", CultureInfo.InvariantCulture)}i"
                : $"{real.ToString("G17", CultureInfo.InvariantCulture)}{(imaginary < 0 ? string.Empty : "+")}{imaginary.ToString("G17", CultureInfo.InvariantCulture)}i";

        return new ComplexNumber(real, imaginary, value.Magnitude, value.Phase, text);
    }

    private static double CleanZero(double value)
    {
        return Math.Abs(value) < 1e-12 ? 0 : value;
    }

    private static double RadToDeg(double radians)
    {
        return radians * 180 / Math.PI;
    }

    private static double Square(double value)
    {
        return value * value;
    }
}
