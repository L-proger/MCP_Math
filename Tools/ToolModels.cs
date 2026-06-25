namespace MCP_Math;

public sealed record ComplexNumber(double Real, double Imaginary, double Magnitude, double PhaseRadians, string Value);

public sealed record PlotPoint(double X, double Y);

public sealed record StatisticsResult(
    int Count,
    double Sum,
    double Mean,
    double Median,
    double[] Mode,
    double Min,
    double Max,
    double PopulationVariance,
    double PopulationStandardDeviation,
    double SampleVariance,
    double SampleStandardDeviation,
    double Q1,
    double Q3);

public sealed record UnitInfo(string Category, double ToBaseFactor);

public sealed record TriangleResult(double a, double b, double c, double A, double B, double C, string[] Notes);

public sealed record PrimeFactor(long Factor, int Exponent);

public sealed record ArithmeticArrayResult(double[] Numbers, double Result);

public sealed record MinMaxResult(double[] Numbers, double Min, double Max);

public sealed record UnaryNumberResult(double Value, double Result);
