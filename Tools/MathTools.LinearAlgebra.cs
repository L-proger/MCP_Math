using System.ComponentModel;
using MathNet.Numerics.LinearAlgebra;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace MCP_Math;

public sealed partial class MathTools
{
    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Perform matrix operations: add, subtract, multiply, determinant, inverse, transpose, eigenvalues.")]
    public static object matrix_operations(
        [Description("First matrix as a rectangular array of rows.")]
        double[][] matrix1,
        [Description("Operation: add, subtract, multiply, determinant, inverse, transpose, or eigenvalues.")]
        string operation,
        [Description("Second matrix for binary operations.")]
        double[][]? matrix2 = null)
    {
        try
        {
            var left = ToMatrix(matrix1, nameof(matrix1));
            var op = NormalizeOperation(operation);

            object result = op switch
            {
                "add" => (left + RequireSecondMatrix(matrix2, left.RowCount, left.ColumnCount, op)).ToRowArrays(),
                "subtract" => (left - RequireSecondMatrix(matrix2, left.RowCount, left.ColumnCount, op)).ToRowArrays(),
                "multiply" => (left * ToMatrix(RequireMatrix(matrix2, nameof(matrix2)), nameof(matrix2))).ToRowArrays(),
                "determinant" => RequireSquare(left, op).Determinant(),
                "inverse" => RequireSquare(left, op).Inverse().ToRowArrays(),
                "transpose" => left.Transpose().ToRowArrays(),
                "eigenvalues" => RequireSquare(left, op).Evd().EigenValues.Select(FormatComplexValue).ToArray(),
                _ => throw new McpException("Unsupported matrix operation. Use add, subtract, multiply, determinant, inverse, transpose, or eigenvalues.")
            };

            return new { operation = op, result };
        }
        catch (Exception ex) when (ex is not McpException)
        {
            throw ToolError("Could not complete matrix operation.", ex);
        }
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Perform vector operations: add, subtract, dot, cross, magnitude, normalize, or angle.")]
    public static object vector_operations(
        [Description("First vector.")]
        double[] vector1,
        [Description("Operation: add, subtract, dot, cross, magnitude, normalize, or angle.")]
        string operation,
        [Description("Second vector for binary operations.")]
        double[]? vector2 = null)
    {
        ValidateVector(vector1, nameof(vector1));
        var op = NormalizeOperation(operation);

        object result = op switch
        {
            "add" => ZipVectors(vector1, RequireVector(vector2, vector1.Length, op), (a, b) => a + b),
            "subtract" => ZipVectors(vector1, RequireVector(vector2, vector1.Length, op), (a, b) => a - b),
            "dot" => Dot(vector1, RequireVector(vector2, vector1.Length, op)),
            "cross" => Cross(vector1, RequireVector(vector2, vector1.Length, op)),
            "magnitude" => Magnitude(vector1),
            "normalize" => NormalizeVector(vector1),
            "angle" => AngleBetween(vector1, RequireVector(vector2, vector1.Length, op)),
            _ => throw new McpException("Unsupported vector operation.")
        };

        return new { operation = op, result };
    }

    private static Matrix<double> ToMatrix(double[][] matrix, string name)
    {
        RequireMatrix(matrix, name);

        var width = matrix[0].Length;
        for (var row = 0; row < matrix.Length; row++)
        {
            if (matrix[row].Length != width)
                throw new McpException($"{name} must be rectangular.");
            if (matrix[row].Any(value => !double.IsFinite(value)))
                throw new McpException($"{name} contains a non-finite value.");
        }

        return Matrix<double>.Build.DenseOfRowArrays(matrix);
    }

    private static double[][] RequireMatrix(double[][]? matrix, string name)
    {
        if (matrix is null || matrix.Length == 0 || matrix.Any(row => row is null || row.Length == 0))
            throw new McpException($"{name} must be a non-empty rectangular matrix.");

        return matrix;
    }

    private static Matrix<double> RequireSecondMatrix(double[][]? matrix, int rows, int columns, string operation)
    {
        var right = ToMatrix(RequireMatrix(matrix, nameof(matrix)), nameof(matrix));
        if (right.RowCount != rows || right.ColumnCount != columns)
            throw new McpException($"{operation} requires matrices with the same dimensions.");

        return right;
    }

    private static Matrix<double> RequireSquare(Matrix<double> matrix, string operation)
    {
        if (matrix.RowCount != matrix.ColumnCount)
            throw new McpException($"{operation} requires a square matrix.");

        return matrix;
    }

    private static void ValidateVector(double[] vector, string name)
    {
        if (vector.Length == 0)
            throw new McpException($"{name} must not be empty.");
        if (vector.Any(value => !double.IsFinite(value)))
            throw new McpException($"{name} contains a non-finite value.");
    }

    private static double[] RequireVector(double[]? vector, int length, string operation)
    {
        if (vector is null)
            throw new McpException($"{operation} requires vector2.");
        ValidateVector(vector, nameof(vector));
        if (vector.Length != length)
            throw new McpException($"{operation} requires vectors with the same length.");

        return vector;
    }

    private static double[] ZipVectors(double[] left, double[] right, Func<double, double, double> op)
    {
        return left.Zip(right, op).ToArray();
    }

    private static double Dot(double[] left, double[] right)
    {
        return left.Zip(right, (a, b) => a * b).Sum();
    }

    private static double[] Cross(double[] left, double[] right)
    {
        if (left.Length != 3 || right.Length != 3)
            throw new McpException("cross requires 3D vectors.");

        return
        [
            left[1] * right[2] - left[2] * right[1],
            left[2] * right[0] - left[0] * right[2],
            left[0] * right[1] - left[1] * right[0]
        ];
    }

    private static double Magnitude(double[] vector)
    {
        return Math.Sqrt(vector.Select(Square).Sum());
    }

    private static double[] NormalizeVector(double[] vector)
    {
        var magnitude = Magnitude(vector);
        if (magnitude == 0)
            throw new McpException("Cannot normalize a zero vector.");

        return vector.Select(x => x / magnitude).ToArray();
    }

    private static double AngleBetween(double[] left, double[] right)
    {
        var denominator = Magnitude(left) * Magnitude(right);
        if (denominator == 0)
            throw new McpException("Cannot compute an angle involving a zero vector.");

        var cosine = Dot(left, right) / denominator;
        return RadToDeg(Math.Acos(Math.Clamp(cosine, -1, 1)));
    }
}
