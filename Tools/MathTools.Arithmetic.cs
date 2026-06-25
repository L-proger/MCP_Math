using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Add a list of numbers.")]
    public static object add_numbers(
        [Description("Numbers to add. Must contain at least one finite number.")]
        double[] numbers)
    {
        ValidateNumbers(numbers, nameof(numbers), requireNonEmpty: true);
        return new ArithmeticArrayResult(numbers, numbers.Sum());
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Subtract one or more numbers from an initial value.")]
    public static object subtract_numbers(
        [Description("Initial value.")]
        double minuend,
        [Description("Numbers to subtract from the initial value.")]
        double[] subtrahends)
    {
        ValidateFinite(minuend, nameof(minuend));
        ValidateNumbers(subtrahends, nameof(subtrahends), requireNonEmpty: false);

        return new
        {
            minuend,
            subtrahends,
            result = subtrahends.Aggregate(minuend, (current, value) => current - value)
        };
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Multiply a list of numbers.")]
    public static object multiply_numbers(
        [Description("Numbers to multiply. Must contain at least one finite number.")]
        double[] numbers)
    {
        ValidateNumbers(numbers, nameof(numbers), requireNonEmpty: true);
        return new ArithmeticArrayResult(numbers, numbers.Aggregate(1d, (current, value) => current * value));
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Divide an initial value by one or more divisors.")]
    public static object divide_numbers(
        [Description("Initial value to divide.")]
        double dividend,
        [Description("Divisors to apply in order. Cannot contain zero.")]
        double[] divisors)
    {
        ValidateFinite(dividend, nameof(dividend));
        ValidateNumbers(divisors, nameof(divisors), requireNonEmpty: true);
        if (divisors.Any(value => value == 0))
            throw new McpException("Divisors cannot contain zero.");

        return new
        {
            dividend,
            divisors,
            result = divisors.Aggregate(dividend, (current, value) => current / value)
        };
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute the arithmetic mean of a list of numbers.")]
    public static object average_numbers(
        [Description("Numbers to average. Must contain at least one finite number.")]
        double[] numbers)
    {
        ValidateNumbers(numbers, nameof(numbers), requireNonEmpty: true);

        return new ArithmeticArrayResult(numbers, numbers.Average());
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Find the smallest number in a list.")]
    public static object min_number(
        [Description("Numbers to inspect. Must contain at least one finite number.")]
        double[] numbers)
    {
        ValidateNumbers(numbers, nameof(numbers), requireNonEmpty: true);
        return new ArithmeticArrayResult(numbers, numbers.Min());
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Find the largest number in a list.")]
    public static object max_number(
        [Description("Numbers to inspect. Must contain at least one finite number.")]
        double[] numbers)
    {
        ValidateNumbers(numbers, nameof(numbers), requireNonEmpty: true);
        return new ArithmeticArrayResult(numbers, numbers.Max());
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Find the smallest and largest numbers in a list.")]
    public static object min_max_numbers(
        [Description("Numbers to inspect. Must contain at least one finite number.")]
        double[] numbers)
    {
        ValidateNumbers(numbers, nameof(numbers), requireNonEmpty: true);
        return new MinMaxResult(numbers, numbers.Min(), numbers.Max());
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Round a number to a specified number of decimal digits.")]
    public static object round_number(
        [Description("Value to round.")]
        double value,
        [Description("Number of decimal digits. Must be between 0 and 15.")]
        int digits = 0)
    {
        ValidateFinite(value, nameof(value));
        if (digits is < 0 or > 15)
            throw new McpException("Digits must be between 0 and 15.");

        return new UnaryNumberResult(value, Math.Round(value, digits, MidpointRounding.AwayFromZero));
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute a percentage of a value, for example 20 percent of 150.")]
    public static object percentage(
        [Description("Base value.")]
        double value,
        [Description("Percentage to apply.")]
        double percent)
    {
        ValidateFinite(value, nameof(value));
        ValidateFinite(percent, nameof(percent));

        return new
        {
            value,
            percent,
            result = value * percent / 100
        };
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute percentage change from one value to another.")]
    public static object percentage_change(
        [Description("Starting value. Cannot be zero.")]
        double from,
        [Description("Ending value.")]
        double to)
    {
        ValidateFinite(from, nameof(from));
        ValidateFinite(to, nameof(to));
        if (from == 0)
            throw new McpException("From value cannot be zero.");

        return new
        {
            from,
            to,
            result_percent = (to - from) / from * 100
        };
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Raise a number to a power.")]
    public static object power_number(
        [Description("Base value.")]
        double @base,
        [Description("Exponent value.")]
        double exponent)
    {
        ValidateFinite(@base, nameof(@base));
        ValidateFinite(exponent, nameof(exponent));

        return new
        {
            @base,
            exponent,
            result = Math.Pow(@base, exponent)
        };
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute a root of a number, square root by default.")]
    public static object root_number(
        [Description("Value whose root should be computed.")]
        double value,
        [Description("Root degree. Must be non-zero.")]
        double degree = 2)
    {
        ValidateFinite(value, nameof(value));
        ValidateFinite(degree, nameof(degree));
        if (degree == 0)
            throw new McpException("Degree cannot be zero.");
        if (value < 0 && !IsOddInteger(degree))
            throw new McpException("Negative values require an odd integer degree.");

        var result = value < 0 ? -Math.Pow(-value, 1 / degree) : Math.Pow(value, 1 / degree);
        return new
        {
            value,
            degree,
            result
        };
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute the absolute value of a number.")]
    public static object absolute_number(
        [Description("Value whose absolute value should be computed.")]
        double value)
    {
        ValidateFinite(value, nameof(value));
        return new UnaryNumberResult(value, Math.Abs(value));
    }

    private static void ValidateNumbers(double[] numbers, string name, bool requireNonEmpty)
    {
        if (requireNonEmpty && numbers.Length == 0)
            throw new McpException($"{name} must contain at least one number.");
        if (numbers.Any(value => !double.IsFinite(value)))
            throw new McpException($"{name} can only contain finite numbers.");
    }

    private static void ValidateFinite(double value, string name)
    {
        if (!double.IsFinite(value))
            throw new McpException($"{name} must be finite.");
    }

    private static bool IsOddInteger(double value)
    {
        return Math.Abs(value % 2) == 1 && Math.Abs(value - Math.Round(value)) < 1e-12;
    }
}
