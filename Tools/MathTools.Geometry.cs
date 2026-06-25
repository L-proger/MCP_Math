using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Solve a triangle from known sides a,b,c and angles A,B,C in degrees.")]
    public static object solve_triangle(
        [Description("Side a opposite angle A.")]
        double? a = null,
        [Description("Side b opposite angle B.")]
        double? b = null,
        [Description("Side c opposite angle C.")]
        double? c = null,
        [Description("Angle A in degrees.")]
        double? A = null,
        [Description("Angle B in degrees.")]
        double? B = null,
        [Description("Angle C in degrees.")]
        double? C = null)
    {
        try
        {
            var triangle = SolveTriangle(a, b, c, A, B, C);
            return triangle;
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not solve triangle.", ex);
        }
    }

    private static TriangleResult SolveTriangle(double? a, double? b, double? c, double? A, double? B, double? C)
    {
        ValidatePositive(a, nameof(a));
        ValidatePositive(b, nameof(b));
        ValidatePositive(c, nameof(c));
        ValidateAngle(A, nameof(A));
        ValidateAngle(B, nameof(B));
        ValidateAngle(C, nameof(C));

        var sides = new double?[] { a, b, c };
        var angles = new double?[] { A, B, C };
        var notes = new List<string>();

        if (sides.Count(x => x.HasValue) + angles.Count(x => x.HasValue) < 3)
            throw new McpException("At least three triangle values are required.");

        for (var pass = 0; pass < 10; pass++)
        {
            var changed = false;

            if (angles.Count(x => x.HasValue) == 2)
            {
                var missing = Array.FindIndex(angles, x => !x.HasValue);
                if (missing >= 0)
                {
                    angles[missing] = 180 - angles.Where(x => x.HasValue).Sum(x => x!.Value);
                    changed = true;
                }
            }

            if (sides.All(x => x.HasValue))
            {
                for (var i = 0; i < 3; i++)
                {
                    if (!angles[i].HasValue)
                    {
                        angles[i] = AngleFromSides(sides[i]!.Value, sides[(i + 1) % 3]!.Value, sides[(i + 2) % 3]!.Value);
                        changed = true;
                    }
                }
            }

            for (var i = 0; i < 3; i++)
            {
                if (sides[i].HasValue && angles[i].HasValue)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        if (!sides[j].HasValue && angles[j].HasValue)
                        {
                            sides[j] = sides[i]!.Value * SinDeg(angles[j]!.Value) / SinDeg(angles[i]!.Value);
                            changed = true;
                        }
                        else if (sides[j].HasValue && !angles[j].HasValue)
                        {
                            var ratio = sides[j]!.Value * SinDeg(angles[i]!.Value) / sides[i]!.Value;
                            if (ratio is >= -1 and <= 1)
                            {
                                angles[j] = RadToDeg(Math.Asin(Math.Clamp(ratio, -1, 1)));
                                notes.Add("SSA data can be ambiguous; returned the principal angle solution.");
                                changed = true;
                            }
                        }
                    }
                }
            }

            if (!changed)
                break;
        }

        if (!sides.All(x => x.HasValue) || !angles.All(x => x.HasValue))
            throw new McpException("Could not determine a unique triangle from the supplied values.");
        if (Math.Abs(angles.Sum(x => x!.Value) - 180) > 1e-6)
            throw new McpException("Angles do not sum to 180 degrees.");

        return new TriangleResult(
            sides[0]!.Value,
            sides[1]!.Value,
            sides[2]!.Value,
            angles[0]!.Value,
            angles[1]!.Value,
            angles[2]!.Value,
            notes.Distinct().ToArray());
    }

    private static void ValidatePositive(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
            throw new McpException($"{name} must be a positive finite number.");
    }

    private static void ValidateAngle(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0 || value.Value >= 180))
            throw new McpException($"{name} must be between 0 and 180 degrees.");
    }

    private static double AngleFromSides(double opposite, double side1, double side2)
    {
        var cosine = (Square(side1) + Square(side2) - Square(opposite)) / (2 * side1 * side2);
        return RadToDeg(Math.Acos(Math.Clamp(cosine, -1, 1)));
    }

    private static double SinDeg(double angle)
    {
        return Math.Sin(angle * Math.PI / 180);
    }
}
