# MCP Math

MCP stdio server with math tools written in C# for .NET 10.

The server exposes small, explicit tools for simple arithmetic, plus tools for expressions, algebra, matrices, vectors, statistics, units, geometry, number theory, complex numbers, and PNG charts.

## Run

```powershell
dotnet run --project MCP_Math.csproj
```

## Standalone Build

Create a self-contained single-file build:

```powershell
.\scripts\build-standalone.ps1
```

Build all recommended standalone packages:

```powershell
.\scripts\build-all-standalone.ps1
```

The default output is:

```text
artifacts/standalone/win-x64/
```

## MCP Config

Add this to your `mcp.json`:

```json
{
  "mcpServers": {
    "math": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/MCP.Math/MCP_Math.csproj"
      ]
    }
  }
}
```

After the first build, you can use `--no-build`:

```json
{
  "mcpServers": {
    "math": {
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "/path/to/MCP.Math/MCP_Math.csproj"
      ]
    }
  }
}
```

For a standalone build, point the MCP client directly at the published executable:

```json
{
  "mcpServers": {
    "math": {
      "command": "/path/to/MCP.Math/artifacts/standalone/win-x64/MCP_Math.exe"
    }
  }
}
```

## Tool Groups

- Arithmetic: exact sum of number arrays/lists (`sum_numbers`), subtract, multiply, divide, average, min/max, rounding, percentages, powers, roots, absolute value.
- Expressions and algebra: calculate, simplify, expand, factorize, solve equations/systems, derivative, integral, limit, summation.
- Linear algebra: matrix operations and vector operations.
- Analysis helpers: statistics, unit conversion, triangle solving, prime factorization, GCD/LCM, complex number operations.
- Plotting helpers: sample a function into points or return PNG image charts for expressions, multiple expressions, points, scatter plots, bar charts, histograms, polar plots, heatmaps, time series, error bars, box plots, step plots, area charts, and pie charts.

## Expression Notes

Expression tools are powered by AngouriMath.

Use explicit multiplication:

```text
2 * x
```

Powers can be written as either:

```text
x^2
x**2
```

Some symbolic operations may return an unresolved expression when the math engine cannot solve the expression exactly.



