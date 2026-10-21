using CoreRentalNet.Evaluation.Cases;

namespace CoreRentalNet.Evaluation.Configuration;

/// <summary>What one evaluation run needs to know.</summary>
public sealed record EvaluationOptions
{
    public string TestScriptPath { get; init; } = "Test_Script.md";

    public string ProductsPath { get; init; } = "products.json";

    public string TraceDirectory { get; init; } = "artifacts/evaluation/traces";

    public EvaluationScope Scope { get; init; } = EvaluationScope.Tiny;

    public int Repetitions { get; init; } = 3;

    /// <summary>How long one case execution may take before it is recorded as timed out.</summary>
    public TimeSpan CaseTimeout { get; init; } = TimeSpan.FromSeconds(15);

    public int MaximumSelectedProductsPerComponent { get; init; } = 3;

    public Uri? AgentEndpoint { get; init; }

    public string AgentName { get; init; } = "core-rental-workspace-suggestion-agent";

    public EvaluationAuthMode AuthMode { get; init; } = EvaluationAuthMode.LocalProvider;

    public string AuthAuthority { get; init; } = "http://127.0.0.1:8090";

    public string AuthClientId { get; init; } = "core-rental-e2e";

    public string AuthAccount { get; init; } = "reader";

    public string AuthRedirectUri { get; init; } = "http://127.0.0.1:5199/signin-oidc";

    public TimeSpan TokenRefreshMargin { get; init; } = TimeSpan.FromSeconds(30);

    public string? CallerToken { get; init; }

    public Uri? McpEndpoint { get; init; }

    public Uri? JudgeEndpoint { get; init; }

    public string JudgeModel { get; init; } = "gpt-4.1-mini";

    public string ReportPath { get; init; } = "specs/evaluation/reports/latest.json";
}
