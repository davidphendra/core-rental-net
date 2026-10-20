using Azure.AI.Projects;
using Azure.Identity;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Configuration;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;
using CoreRentalNet.Evaluation.Reporting;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Runs the 200 cases through the real agent and writes the report.</summary>
public static class RunEvaluationCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var options = Parse(args);

        if (options.AgentEndpoint is null)
        {
            Console.Error.WriteLine("--agent-endpoint is required.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(options.CallerToken))
        {
            Console.Error.WriteLine("--caller-token or EVALUATION_CALLER_TOKEN is required.");
            return 1;
        }

        var loaded = CaseSetLoader.Load(options.TestScriptPath, options.ProductsPath);

        if (!loaded.Validation.IsValid)
        {
            Console.Error.WriteLine("The fixture is invalid; run validate-cases first.");
            return 1;
        }

        var agent = new AgentHttpClient(options.AgentEndpoint, options.AgentName, options.CallerToken);
        var traceReader = new EvaluationTraceReader(options.TraceDirectory);
        var runner = new EvaluationRunner(agent, traceReader);
        var projector = new WorkspaceTraceProjector(options.MaximumSelectedProductsPerComponent);
        var contextFactory = new EvaluationContextFactory(projector, SlotCapacities.Default);

        IToolCatalog toolCatalog = options.McpEndpoint is null || string.IsNullOrWhiteSpace(options.CallerToken)
            ? new EmptyToolCatalog()
            : new McpToolCatalog(options.McpEndpoint, options.CallerToken);

        var itemFactory = new EvalItemFactory(toolCatalog, contextFactory);

        var evaluators = new List<IAgentEvaluator>
        {
            EvaluatorFactory.CreateLocal(loaded.Catalogue),
        };

        if (options.JudgeEndpoint is not null)
        {
            var projectClient = new AIProjectClient(
                options.JudgeEndpoint,
                new AzureDeveloperCliCredential());

            evaluators.Add(EvaluatorFactory.CreateFoundry(projectClient, options.JudgeModel));
        }
        else
        {
            Console.WriteLine("Foundry judge not configured; running deterministic checks only.");
        }

        var pipeline = new EvaluationPipeline(itemFactory, evaluators, new EvaluationReportFactory());

        Console.WriteLine($"Running {loaded.Cases.Cases.Count} cases x {options.Repetitions} repetitions...");
        var runs = await runner.RunAsync(loaded.Cases, options.Repetitions, cancellationToken);
        var report = await pipeline.EvaluateAsync(runs, cancellationToken);

        var reportDirectory = Path.GetDirectoryName(options.ReportPath);
        if (!string.IsNullOrEmpty(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        await JsonReportWriter.WriteAsync(report, options.ReportPath, cancellationToken);
        await MarkdownReportWriter.WriteAsync(report, Path.ChangeExtension(options.ReportPath, ".md"), cancellationToken);

        Console.WriteLine($"Rows: {report.Summary.Rows}");
        Console.WriteLine($"Executions: {report.Summary.Executions}");
        Console.WriteLine($"Case pass: {report.Summary.CasePass}/{report.Summary.Rows} ({report.Summary.CasePassRate:P1})");
        Console.WriteLine($"Report: {options.ReportPath}");

        return report.Summary.CasePass == report.Summary.Rows ? 0 : 1;
    }

    private static EvaluationOptions Parse(string[] args)
    {
        var options = new EvaluationOptions
        {
            TestScriptPath = Path.Combine(AppContext.BaseDirectory, "Test_Script.md"),
            ProductsPath = Path.Combine(AppContext.BaseDirectory, "products.json"),
            JudgeEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") is { Length: > 0 } endpoint
                ? new Uri(endpoint)
                : null,
            JudgeModel = "gpt-4.1-mini",
            CallerToken = Environment.GetEnvironmentVariable("EVALUATION_CALLER_TOKEN"),
            ReportPath = "specs/evaluation/reports/latest.json",
        };

        for (var index = 0; index < args.Length; index++)
        {
            var value = args.ElementAtOrDefault(index + 1);

            switch (args[index])
            {
                case "--repetitions" when value is not null && int.TryParse(value, out var repetitions):
                    options = options with { Repetitions = repetitions };
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
}
