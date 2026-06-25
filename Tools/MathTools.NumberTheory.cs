using System.ComponentModel;
using System.Numerics;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute prime factorization for an integer.")]
    public static object prime_factorization(
        [Description("Integer to factorize.")]
        long n)
    {
        if (n == 0)
            throw new McpException("Zero does not have a prime factorization.");

        var remaining = Math.Abs(n);
        var factors = new List<PrimeFactor>();

        if (n < 0)
            factors.Add(new PrimeFactor(-1, 1));

        var exponent = 0;
        while (remaining % 2 == 0)
        {
            exponent++;
            remaining /= 2;
        }
        if (exponent > 0)
            factors.Add(new PrimeFactor(2, exponent));

        for (long divisor = 3; divisor <= remaining / divisor; divisor += 2)
        {
            exponent = 0;
            while (remaining % divisor == 0)
            {
                exponent++;
                remaining /= divisor;
            }
            if (exponent > 0)
                factors.Add(new PrimeFactor(divisor, exponent));
        }

        if (remaining > 1)
            factors.Add(new PrimeFactor(remaining, 1));

        return new { n, factors };
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute GCD and LCM for a list of integers.")]
    public static object gcd_lcm(
        [Description("Integers to analyze.")]
        long[] numbers)
    {
        if (numbers.Length == 0)
            throw new McpException("At least one number is required.");
        if (numbers.Any(n => n == long.MinValue))
            throw new McpException("long.MinValue is not supported.");

        var gcd = numbers.Select(Math.Abs).Aggregate(Gcd);
        BigInteger lcm = 1;
        foreach (var number in numbers.Select(Math.Abs))
        {
            if (number == 0)
            {
                lcm = 0;
                break;
            }

            lcm = BigInteger.Abs(lcm / Gcd((long)BigInteger.Abs(lcm), number) * number);
        }

        return new
        {
            numbers,
            gcd,
            lcm = lcm.ToString()
        };
    }

    private static long Gcd(long a, long b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);

        while (b != 0)
        {
            var temp = a % b;
            a = b;
            b = temp;
        }

        return a;
    }
}
