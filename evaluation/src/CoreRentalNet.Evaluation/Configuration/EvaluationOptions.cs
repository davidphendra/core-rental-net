namespace CoreRentalNet.Evaluation.Configuration;

/// <summary>What one evaluation run needs to know.</summary>
public sealed record EvaluationOptions
{
    public string TestScriptPath { get; init; } = "Test_Script.md";

    public string ProductsPath { get; init; } = "products.json";

    public string TraceDirectory { get; init; } = "artifacts/evaluation/traces";

    public int Repetitions { get; init; } = 3;

    public int MaximumSelectedProductsPerComponent { get; init; } = 3;

    public Uri? AgentEndpoint { get; init; }

    public string AgentName { get; init; } = "core-rental-workspace-suggestion-agent";

    public string? CallerToken { get; init; }

    public Uri? McpEndpoint { get; init; }

    public Uri? JudgeEndpoint { get; init; }

    public string JudgeModel { get; init; } = "gpt-4.1-mini";

    public string ReportPath { get; init; } = "specs/evaluation/reports/latest.json";
}
