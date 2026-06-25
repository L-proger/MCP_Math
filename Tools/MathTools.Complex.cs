using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Perform complex number operations. Complex values may be like 3+4i, 3-4j, i, or (3,4).")]
    public static object complex_operations(
        [Description("First complex number.")]
        string z1,
        [Description("Operation: add, subtract, multiply, divide, power, sqrt, conjugate, magnitude, phase, real, imaginary, exp, or log.")]
        string operation,
        [Description("Second complex number for binary operations.")]
        string? z2 = null)
    {
        var left = ParseComplex(z1);
        var op = NormalizeOperation(operation);

        object result = op switch
        {
            "add" => FormatComplexValue(left + RequireComplex(z2, op)),
            "subtract" => FormatComplexValue(left - RequireComplex(z2, op)),
            "multiply" => FormatComplexValue(left * RequireComplex(z2, op)),
            "divide" => FormatComplexValue(left / RequireComplex(z2, op)),
            "power" => FormatComplexValue(Complex.Pow(left, RequireComplex(z2, op))),
            "sqrt" => FormatComplexValue(Complex.Sqrt(left)),
            "conjugate" => FormatComplexValue(Complex.Conjugate(left)),
            "magnitude" => left.Magnitude,
            "phase" => left.Phase,
            "real" => left.Real,
            "imaginary" => left.Imaginary,
            "exp" => FormatComplexValue(Complex.Exp(left)),
            "log" => FormatComplexValue(Complex.Log(left)),
            _ => throw new McpException("Unsupported complex operation.")
        };

        return new { z1, z2, operation = op, result };
    }

    private static Complex ParseComplex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new McpException("Complex number cannot be empty.");

        var value = text.Trim().ToLowerInvariant().Replace(" ", string.Empty, StringComparison.Ordinal).Replace('j', 'i');
        if (value.StartsWith('(') && value.EndsWith(')') && value.Contains(','))
        {
            var parts = value[1..^1].Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
                return new Complex(ParseDouble(parts[0]), ParseDouble(parts[1]));
        }

        if (!value.Contains('i'))
            return new Complex(ParseDouble(value), 0);
        if (!value.EndsWith('i'))
            throw new McpException("Complex number must put i at the end of the imaginary term.");

        var withoutI = value[..^1];
        var splitAt = FindRealImaginarySplit(withoutI);

        if (splitAt < 0)
            return new Complex(0, ParseImaginary(withoutI));

        var real = ParseDouble(withoutI[..splitAt]);
        var imaginary = ParseImaginary(withoutI[splitAt..]);
        return new Complex(real, imaginary);
    }

    private static int FindRealImaginarySplit(string value)
    {
        for (var i = value.Length - 1; i > 0; i--)
        {
            if ((value[i] == '+' || value[i] == '-') && value[i - 1] != 'e')
                return i;
        }

        return -1;
    }

    private static double ParseImaginary(string value)
    {
        return value switch
        {
            "" or "+" => 1,
            "-" => -1,
            _ => ParseDouble(value)
        };
    }

    private static double ParseDouble(string text)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new McpException($"Could not parse '{text}' as a number.");
        if (!double.IsFinite(value))
            throw new McpException("Number must be finite.");

        return value;
    }

    private static Complex RequireComplex(string? value, string operation)
    {
        if (value is null)
            throw new McpException($"{operation} requires z2.");

        return ParseComplex(value);
    }
}
