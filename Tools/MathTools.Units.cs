using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Convert a value between common length, mass, time, and temperature units.")]
    public static object unit_conversion(
        [Description("Numeric value to convert.")]
        double value,
        [Description("Source unit, for example m, cm, km, in, ft, kg, lb, s, min, h, c, f, or k.")]
        string from_unit,
        [Description("Target unit.")]
        string to_unit)
    {
        if (!double.IsFinite(value))
            throw new McpException("Value must be finite.");

        var result = ConvertUnit(value, from_unit, to_unit);
        return new
        {
            value,
            from_unit,
            to_unit,
            result
        };
    }

    private static double ConvertUnit(double value, string fromUnit, string toUnit)
    {
        var from = NormalizeUnit(fromUnit);
        var to = NormalizeUnit(toUnit);

        if (TemperatureUnits.Contains(from) || TemperatureUnits.Contains(to))
            return ConvertTemperature(value, from, to);

        if (!UnitFactors.TryGetValue(from, out var fromInfo))
            throw new McpException($"Unsupported source unit '{fromUnit}'.");
        if (!UnitFactors.TryGetValue(to, out var toInfo))
            throw new McpException($"Unsupported target unit '{toUnit}'.");
        if (!StringComparer.Ordinal.Equals(fromInfo.Category, toInfo.Category))
            throw new McpException($"Cannot convert from {fromUnit} to {toUnit}; unit categories differ.");

        var baseValue = value * fromInfo.ToBaseFactor;
        return baseValue / toInfo.ToBaseFactor;
    }

    private static string NormalizeUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new McpException("Unit cannot be empty.");

        return unit.Trim().ToLowerInvariant();
    }

    private static double ConvertTemperature(double value, string from, string to)
    {
        if (!TemperatureUnits.Contains(from) || !TemperatureUnits.Contains(to))
            throw new McpException("Temperature units can only be converted to other temperature units.");

        var kelvin = from switch
        {
            "c" or "celsius" => value + 273.15,
            "f" or "fahrenheit" => (value - 32) * 5 / 9 + 273.15,
            "k" or "kelvin" => value,
            _ => throw new McpException($"Unsupported temperature unit '{from}'.")
        };

        return to switch
        {
            "c" or "celsius" => kelvin - 273.15,
            "f" or "fahrenheit" => (kelvin - 273.15) * 9 / 5 + 32,
            "k" or "kelvin" => kelvin,
            _ => throw new McpException($"Unsupported temperature unit '{to}'.")
        };
    }

    private static readonly Dictionary<string, UnitInfo> UnitFactors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["m"] = new("length", 1),
        ["meter"] = new("length", 1),
        ["meters"] = new("length", 1),
        ["cm"] = new("length", 0.01),
        ["centimeter"] = new("length", 0.01),
        ["centimeters"] = new("length", 0.01),
        ["mm"] = new("length", 0.001),
        ["millimeter"] = new("length", 0.001),
        ["millimeters"] = new("length", 0.001),
        ["km"] = new("length", 1000),
        ["kilometer"] = new("length", 1000),
        ["kilometers"] = new("length", 1000),
        ["in"] = new("length", 0.0254),
        ["inch"] = new("length", 0.0254),
        ["inches"] = new("length", 0.0254),
        ["ft"] = new("length", 0.3048),
        ["foot"] = new("length", 0.3048),
        ["feet"] = new("length", 0.3048),
        ["yd"] = new("length", 0.9144),
        ["yard"] = new("length", 0.9144),
        ["yards"] = new("length", 0.9144),
        ["mi"] = new("length", 1609.344),
        ["mile"] = new("length", 1609.344),
        ["miles"] = new("length", 1609.344),

        ["kg"] = new("mass", 1),
        ["kilogram"] = new("mass", 1),
        ["kilograms"] = new("mass", 1),
        ["g"] = new("mass", 0.001),
        ["gram"] = new("mass", 0.001),
        ["grams"] = new("mass", 0.001),
        ["mg"] = new("mass", 0.000001),
        ["milligram"] = new("mass", 0.000001),
        ["milligrams"] = new("mass", 0.000001),
        ["lb"] = new("mass", 0.45359237),
        ["pound"] = new("mass", 0.45359237),
        ["pounds"] = new("mass", 0.45359237),
        ["oz"] = new("mass", 0.028349523125),
        ["ounce"] = new("mass", 0.028349523125),
        ["ounces"] = new("mass", 0.028349523125),

        ["s"] = new("time", 1),
        ["sec"] = new("time", 1),
        ["second"] = new("time", 1),
        ["seconds"] = new("time", 1),
        ["min"] = new("time", 60),
        ["minute"] = new("time", 60),
        ["minutes"] = new("time", 60),
        ["h"] = new("time", 3600),
        ["hr"] = new("time", 3600),
        ["hour"] = new("time", 3600),
        ["hours"] = new("time", 3600),
        ["day"] = new("time", 86400),
        ["days"] = new("time", 86400)
    };

    private static readonly HashSet<string> TemperatureUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "c", "celsius", "f", "fahrenheit", "k", "kelvin"
    };
}
