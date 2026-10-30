using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Configuration;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evaluation.AgentEval;
using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;
using CoreRentalNet.Evaluation.Reporting;
using CoreRentalNet.Evaluation.Reporting.Bias;
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

        if (options.CaseId is { } caseId)
        {
            selected = EvaluationCaseSetSelector.SelectCase(selected, caseId);
        }

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

        var judgeClient = EvaluationJudgeFactory.CreateClient(options);
        var judgeConfigured = judgeClient is not null;

        if (judgeClient is not null)
        {
            if (options.JudgeMode is JudgeMode.Rag or JudgeMode.Both)
            {
                evaluators.Add(new ProjectingEvaluator(
                    AgentEvalRagEvaluatorFactory.Create(judgeClient),
                    new AgentEvalRagContextProjector()));
            }

            if (options.JudgeMode is JudgeMode.Equivalence or JudgeMode.Both)
            {
                evaluators.Add(EvaluationJudgeFactory.CreateEquivalenceJudge(
                    judgeClient,
                    options.JudgeTimeout,
                    options.JudgeRetries));
            }
        }
        else
        {
            Console.WriteLine("Judge not configured; running deterministic checks only.");
        }

        var pipeline = new EvaluationPipeline(
            itemFactory,
            evaluators,
            new EvaluationReportFactory(
                loaded.Catalogue,
                BiasReportBuilder.CreateDefault(),
                judgeConfigured));
        var agent = new AgentHttpClient(options.AgentEndpoint, options.AgentName);
        var traceReader = new EvaluationTraceReader(options.TraceDirectory);
        var runner = new EvaluationRunner(
            agent,
            traceReader,
            tokenProvider,
            options.CaseTimeout,
            options.MaximumConcurrentCases);

        Console.WriteLine(
            $"Running {selected.Cases.Count}/{loaded.Cases.Cases.Count} cases " +
            $"({options.Scope}) x {options.Repetitions} repetitions...");

        IReadOnlyList<RowRun> runs;

        try
        {
            runs = await runner.RunAsync(selected, options.Repetitions, cancellationToken)
                .ConfigureAwait(false);
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
                stopReason: null,
                cancellationToken)
            .ConfigureAwait(false);

        report = report with
        {
            Stochastic = StochasticSummaryFactory.Create(report, options.SuccessRateThreshold),
            PolicyDigest = judgeClient is null
                ? EvaluationPolicy.DeterministicDigest
                : EvaluationPolicy.Digest(options.JudgeMode),
            RagConfigured = judgeClient is not null
                            && options.JudgeMode is JudgeMode.Rag or JudgeMode.Both,
        };

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

        var agentEvalReport = AgentEvalReportProjector.Project(report, options.AgentName);
        agentEvalReport.Print();

        if (options.Exports.Count > 0)
        {
            try
            {
                await AgentEvalReportExporter.WriteAsync(
                        agentEvalReport,
                        options.ReportPath,
                        options.Exports,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 4;
            }
        }

        Console.WriteLine($"Rows: {report.Summary.Rows}");
        Console.WriteLine($"Executions: {report.Summary.Executions}");
        Console.WriteLine($"Case pass: {report.Summary.CasePass}/{report.Summary.Rows} ({report.Summary.CasePassRate:P1})");
        Console.WriteLine($"Report: {options.ReportPath}");

        return report.Summary.CasePass == report.Summary.Rows ? 0 : 1;
    }
}
