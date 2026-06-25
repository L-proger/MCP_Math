using System.ComponentModel;
using AngouriMath;
using AngouriMath.Extensions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

[McpServerToolType]
public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Evaluate a mathematical expression numerically. Use explicit multiplication, for example 2 * x, and ^ or ** for powers.")]
    public static object calculate(
        [Description("Expression to evaluate, for example 2 + 2, sqrt(2), sin(pi / 2), or (1 + 2 * i)^2.")]
        string expression)
    {
        try
        {
            var normalized = NormalizeExpression(expression);
            var number = ParseEntity(normalized).EvalNumerical();
            var complex = number.ToNumerics();

            return new
            {
                expression = normalized,
                exact = number.ToString(),
                value = FormatComplexValue(complex)
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not evaluate expression.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Solve a single equation for a variable.")]
    public static object solve_equation(
        [Description("Equation to solve. Accepts either x^2 - 4 or x^2 = 4.")]
        string equation,
        [Description("Variable to solve for, for example x.")]
        string variable = "x")
    {
        try
        {
            var normalized = NormalizeEquation(equation);
            var result = normalized.SolveEquation(MathS.Var(variable));

            return new
            {
                equation,
                variable,
                roots = result.ToString()
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not solve equation.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Solve a system of equations for the provided variables.")]
    public static object solve_system(
        [Description("Equations to solve. Each equation may be either an expression equal to zero or contain '='.")]
        string[] equations,
        [Description("Variables to solve for. The count should match the number of equations.")]
        string[] variables)
    {
        try
        {
            if (equations.Length == 0)
                throw new McpException("At least one equation is required.");
            if (variables.Length != equations.Length)
                throw new McpException("The number of variables must match the number of equations.");

            var normalized = equations.Select(NormalizeEquation).Select(ParseEntity).ToArray();
            var vars = variables.Select(MathS.Var).ToArray();
            var solutions = MathS.Equations(normalized).Solve(vars);

            return new
            {
                equations,
                variables,
                solutions = solutions is null ? "No solution found." : solutions.ToString(true)
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not solve system.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Differentiate an expression symbolically.")]
    public static object derivative(
        [Description("Expression to differentiate, for example x^3 + sin(x).")]
        string expression,
        [Description("Variable to differentiate by.")]
        string variable = "x",
        [Description("Derivative order. Must be at least 1.")]
        int order = 1)
    {
        try
        {
            if (order < 1)
                throw new McpException("Derivative order must be at least 1.");

            Entity result = ParseEntity(expression);
            var x = MathS.Var(variable);
            for (var i = 0; i < order; i++)
                result = result.Differentiate(x).Simplify();

            return new
            {
                expression = NormalizeExpression(expression),
                variable,
                order,
                result = result.ToString()
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not differentiate expression.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Integrate an expression symbolically. Provide both from and to for a definite integral.")]
    public static object integral(
        [Description("Expression to integrate, for example x^2 or sin(x).")]
        string expression,
        [Description("Variable to integrate by.")]
        string variable = "x",
        [Description("Optional lower bound for a definite integral.")]
        string? from = null,
        [Description("Optional upper bound for a definite integral.")]
        string? to = null)
    {
        try
        {
            var x = MathS.Var(variable);
            var normalized = NormalizeExpression(expression);
            Entity result;

            if (from is null && to is null)
            {
                result = normalized.Integrate(x).Simplify();
            }
            else
            {
                if (from is null || to is null)
                    throw new McpException("Both from and to are required for a definite integral.");

                result = normalized.Integrate(x, ParseEntity(from), ParseEntity(to)).Simplify();
            }

            return new
            {
                expression = normalized,
                variable,
                from,
                to,
                result = result.ToString()
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not integrate expression.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Simplify a symbolic expression.")]
    public static object simplify_expression(
        [Description("Expression to simplify.")]
        string expression)
    {
        try
        {
            var normalized = NormalizeExpression(expression);
            return new { expression = normalized, result = normalized.Simplify().ToString() };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not simplify expression.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Expand products and powers in a symbolic expression.")]
    public static object expand_expression(
        [Description("Expression to expand, for example (x + 1)^3.")]
        string expression)
    {
        try
        {
            var normalized = NormalizeExpression(expression);
            return new { expression = normalized, result = normalized.Expand().ToString() };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not expand expression.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Factorize a symbolic expression.")]
    public static object factorize_expression(
        [Description("Expression to factorize, for example x^2 - 1.")]
        string expression)
    {
        try
        {
            var normalized = NormalizeExpression(expression);
            return new { expression = normalized, result = normalized.Factorize().ToString() };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not factorize expression.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Find the symbolic limit of an expression.")]
    public static object limit(
        [Description("Expression whose limit should be computed.")]
        string expression,
        [Description("Variable approaching the point.")]
        string variable,
        [Description("Point to approach, for example 0, pi, +oo, or -oo.")]
        string point,
        [Description("Approach direction: both, left, or right.")]
        string direction = "both")
    {
        try
        {
            var side = ParseApproach(direction);
            var normalized = NormalizeExpression(expression);
            var result = normalized.Limit(MathS.Var(variable), ParseEntity(point), side).Simplify();

            return new
            {
                expression = normalized,
                variable,
                point,
                direction = side.ToString(),
                result = result.ToString()
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not compute limit.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compute a finite symbolic summation.")]
    public static object summation(
        [Description("Expression to sum, for example k^2.")]
        string expression,
        [Description("Summation variable.")]
        string variable,
        [Description("Inclusive start value.")]
        int start,
        [Description("Inclusive end value.")]
        int end)
    {
        try
        {
            if (end < start)
                throw new McpException("End must be greater than or equal to start.");
            if ((long)end - start > 100_000)
                throw new McpException("Summation range is too large. Limit is 100000 terms.");

            var x = MathS.Var(variable);
            var expr = ParseEntity(expression);
            Entity total = 0;

            for (var i = start; i <= end; i++)
                total += expr.Substitute(x, i);

            var result = total.Simplify();

            return new
            {
                expression = NormalizeExpression(expression),
                variable,
                start,
                end,
                result = result.ToString()
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not compute summation.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sample a function over an x range and return plot-ready points.")]
    public static object plot_function(
        [Description("Function expression in x, for example sin(x) or x^2.")]
        string expression,
        [Description("Minimum x value.")]
        double x_min,
        [Description("Maximum x value.")]
        double x_max,
        [Description("Number of sample points, from 2 to 10000.")]
        int points = 100)
    {
        try
        {
            if (points < 2 || points > 10_000)
                throw new McpException("Points must be between 2 and 10000.");
            if (!double.IsFinite(x_min) || !double.IsFinite(x_max))
                throw new McpException("x_min and x_max must be finite numbers.");
            if (x_max <= x_min)
                throw new McpException("x_max must be greater than x_min.");

            var normalized = NormalizeExpression(expression);
            var expr = ParseEntity(normalized);
            var x = MathS.Var("x");
            var step = (x_max - x_min) / (points - 1);
            var samples = new PlotPoint[points];

            for (var i = 0; i < points; i++)
            {
                var xValue = x_min + step * i;
                var yValue = EvaluateReal(expr, x, xValue);
                samples[i] = new PlotPoint(xValue, yValue);
            }

            return new
            {
                expression = normalized,
                x_min,
                x_max,
                points = samples
            };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not sample function.", ex);
        }
    }
}
