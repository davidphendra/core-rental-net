using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Configuration;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Reads the run command's arguments, refusing a value it cannot use.</summary>
internal static class RunEvaluationOptionsParser
{
    public static EvaluationOptions Parse(string[] args)
    {
        var options = new EvaluationOptions
        {
            TestScriptPath = Path.Combine(AppContext.BaseDirectory, "Test_Script.md"),
            ProductsPath = Path.Combine(AppContext.BaseDirectory, "products.json"),
            CallerToken = Environment.GetEnvironmentVariable("EVALUATION_CALLER_TOKEN"),
            ReportPath = "specs/evaluation/reports/latest.json",
        };

        for (var index = 0; index < args.Length; index++)
        {
            var value = args.ElementAtOrDefault(index + 1);

            switch (args[index])
            {
                case "--scope":
                    if (value is null)
                    {
                        throw new ArgumentException("--scope requires a value.");
                    }

                    options = options with
                    {
                        Scope = EvaluationScopeParser.Parse(value),
                    };
                    index++;
                    break;

                case "--concurrency" when value is not null && int.TryParse(value, out var concurrency):
                    if (concurrency <= 0)
                    {
                        throw new ArgumentException("--concurrency must be greater than zero.");
                    }

                    options = options with { MaximumConcurrentCases = concurrency };
                    index++;
                    break;

                case "--case" when value is not null:
                    options = options with { CaseId = value };
                    index++;
                    break;

                case "--auth-mode":
                    if (value is null)
                    {
                        throw new ArgumentException("--auth-mode requires a value.");
                    }

                    options = options with
                    {
                        AuthMode = AuthMode(value),
                    };
                    index++;
                    break;

                case "--auth-authority" when value is not null:
                    options = options with { AuthAuthority = value };
                    index++;
                    break;

                case "--auth-client-id" when value is not null:
                    options = options with { AuthClientId = value };
                    index++;
                    break;

                case "--auth-account" when value is not null:
                    options = options with { AuthAccount = value };
                    index++;
                    break;

                case "--auth-redirect-uri" when value is not null:
                    options = options with { AuthRedirectUri = value };
                    index++;
                    break;

                case "--repetitions" when value is not null && int.TryParse(value, out var repetitions):
                    options = options with { Repetitions = repetitions };
                    index++;
                    break;

                case "--case-timeout" when value is not null && int.TryParse(value, out var seconds):
                    if (seconds <= 0)
                    {
                        throw new ArgumentException("--case-timeout must be greater than zero seconds.");
                    }

                    options = options with { CaseTimeout = TimeSpan.FromSeconds(seconds) };
                    index++;
                    break;

                case "--agent-endpoint" when value is not null:
                    options = options with { AgentEndpoint = new Uri(value) };
                    index++;
                    break;

                case "--agent-name" when value is not null:
                    options = options with { AgentName = value };
                    index++;
                    break;

                case "--caller-token" when value is not null:
                    options = options with { CallerToken = value };
                    index++;
                    break;

                case "--mcp-endpoint" when value is not null:
                    options = options with { McpEndpoint = new Uri(value) };
                    index++;
                    break;

                case "--judge-endpoint" when value is not null:
                    options = options with { JudgeEndpoint = new Uri(value) };
                    index++;
                    break;

                case "--judge-model" when value is not null:
                    options = options with { JudgeModel = value };
                    index++;
                    break;

                case "--judge-api-key" when value is not null:
                    options = options with { JudgeApiKey = value };
                    index++;
                    break;

                case "--judge-timeout" when value is not null && int.TryParse(value, out var judgeSeconds):
                    if (judgeSeconds <= 0)
                    {
                        throw new ArgumentException("--judge-timeout must be greater than zero seconds.");
                    }

                    options = options with { JudgeTimeout = TimeSpan.FromSeconds(judgeSeconds) };
                    index++;
                    break;

                case "--judge-retries" when value is not null && int.TryParse(value, out var judgeRetries):
                    if (judgeRetries < 0)
                    {
                        throw new ArgumentException("--judge-retries must be zero or greater.");
                    }

                    options = options with { JudgeRetries = judgeRetries };
                    index++;
                    break;

                case "--trace-directory" when value is not null:
                    options = options with { TraceDirectory = value };
                    index++;
                    break;

                case "--report" when value is not null:
                    options = options with { ReportPath = value };
                    index++;
                    break;
            }
        }

        return options;
    }

    private static EvaluationAuthMode AuthMode(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "local" => EvaluationAuthMode.LocalProvider,
            "static" => EvaluationAuthMode.StaticToken,
            _ => throw new ArgumentException(
                $"Unknown auth mode '{value}'. Use local or static."),
        };
}
