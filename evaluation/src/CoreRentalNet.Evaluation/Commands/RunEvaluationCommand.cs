using Azure.AI.Projects;
using Azure.Identity;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Configuration;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;
using CoreRentalNet.Evaluation.Reporting;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Runs the selected cases through the real agent and writes the report.</summary>
public static class RunEvaluationCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        EvaluationOptions options;

        try
        {
            options = RunEvaluationOptionsParser.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 4;
        }

        if (options.AgentEndpoint is null)
        {
            Console.Error.WriteLine("--agent-endpoint is required.");
            return 4;
        }

        if (options.AuthMode == EvaluationAuthMode.StaticToken
            && string.IsNullOrWhiteSpace(options.CallerToken))
        {
            Console.Error.WriteLine("--caller-token is required when --auth-mode static is used.");
            return 2;
        }

        var loaded = CaseSetLoader.Load(options.TestScriptPath, options.ProductsPath);

        if (!loaded.Validation.IsValid)
        {
            Console.Error.WriteLine("The fixture is invalid; run validate-cases first.");
            return 4;
        }

        // Validation covered the whole fixture; only now does the scope reduce what is run.
        var selected = EvaluationCaseSetSelector.Select(loaded.Cases, options.Scope);

        using var tokenHttpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
        });

        ICallerTokenProvider tokenProvider = options.AuthMode switch
        {
            EvaluationAuthMode.LocalProvider => new LocalProviderTokenProvider(
                options,
                tokenHttpClient,
                TimeProvider.System),
            EvaluationAuthMode.StaticToken => new StaticCallerTokenProvider(options.CallerToken!),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.AuthMode, null),
        };

        IReadOnlyList<AITool> tools;

        try
        {
            var token = await tokenProvider.GetAsync(cancellationToken).ConfigureAwait(false);
            IToolCatalog toolCatalog = options.McpEndpoint is null
                ? new EmptyToolCatalog()
                : new McpToolCatalog(options.McpEndpoint);

            tools = await new AuthenticationPreflight(toolCatalog, TimeProvider.System)
                .RunAsync(token, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CallerTokenException exception)
        {
            Console.Error.WriteLine($"Authentication failed: {exception.Message}");
            return 2;
        }
        catch (AuthenticationPreflightException exception)
        {
            Console.Error.WriteLine($"Authentication preflight failed: {exception.Message}");
            return 2;
        }

        var projector = new WorkspaceTraceProjector(options.MaximumSelectedProductsPerComponent);
        var contextFactory = new EvaluationContextFactory(projector, SlotCapacities.Default);
        var itemFactory = new EvalItemFactory(tools, contextFactory);

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
        var agent = new AgentHttpClient(options.AgentEndpoint, options.AgentName);
        var traceReader = new EvaluationTraceReader(options.TraceDirectory);
        var runner = new EvaluationRunner(agent, traceReader, tokenProvider, options.CaseTimeout);

        Console.WriteLine(
            $"Running {selected.Cases.Count}/{loaded.Cases.Cases.Count} cases " +
            $"({options.Scope}) x {options.Repetitions} repetitions...");

        IReadOnlyList<RowRun> runs;
        string? stopReason = null;

        try
        {
            runs = await runner.RunAsync(selected, options.Repetitions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CaseTimeoutException exception)
        {
            runs = exception.Runs;
            stopReason = FailureCodes.CaseTimeout;
            Console.Error.WriteLine($"Stopped: {exception.Message}");
        }
        catch (CallerTokenException exception)
        {
            Console.Error.WriteLine($"Authentication failed: {exception.Message}");
            return 2;
        }

        var report = await pipeline.EvaluateAsync(
                runs,
                options.Scope,
                loaded.Cases.Cases.Count,
                stopReason,
                cancellationToken)
            .ConfigureAwait(false);

        var reportDirectory = Path.GetDirectoryName(options.ReportPath);
        if (!string.IsNullOrEmpty(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        await JsonReportWriter.WriteAsync(report, options.ReportPath, cancellationToken).ConfigureAwait(false);
        await MarkdownReportWriter.WriteAsync(
                report,
                Path.ChangeExtension(options.ReportPath, ".md"),
                cancellationToken)
            .ConfigureAwait(false);

        Console.WriteLine($"Rows: {report.Summary.Rows}");
        Console.WriteLine($"Executions: {report.Summary.Executions}");
        Console.WriteLine($"Case pass: {report.Summary.CasePass}/{report.Summary.Rows} ({report.Summary.CasePassRate:P1})");
        Console.WriteLine($"Report: {options.ReportPath}");

        return stopReason is not null
            ? 3
            : report.Summary.CasePass == report.Summary.Rows ? 0 : 1;
    }
}
