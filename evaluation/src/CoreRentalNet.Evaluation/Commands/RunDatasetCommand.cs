using System.Text.Json;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Configuration;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Commands;

/// <summary>Runs one dataset end to end: load, execute, project, grade, store.</summary>
/// <remarks>
/// A dataset case is bridged to the existing case type only to reach the runner; execution needs nothing but the
/// query, and the bridge deliberately carries no expectations so nothing can leak into the request.
/// </remarks>
public static class RunDatasetCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var name = Value(args, "--dataset", "development");
        var runId = Value(args, "--run-id", $"{name}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmss}");
        var reports = Value(args, "--reports-dir", Path.Combine(RepositoryRoot.Find(), "specs", "evaluation", "reports"));
        var datasetName = Enum.Parse<DatasetName>(name, ignoreCase: true);
        var dataset = await new DatasetLoader()
            .LoadAsync(datasetName, DatasetPaths.Default.For(datasetName))
            .ConfigureAwait(false);
        var validation = new DatasetValidator().Validate(dataset);
        Report(dataset, validation);
        if (!validation.IsValid)
        {
            return 1;
        }

        dataset = Select(dataset, args);
        var violations = LockboxGuard.Violations(
            dataset, args.Contains("--confirm-lockbox"), args.Contains("--re-evaluate-lockbox"), PriorHashes(reports));
        foreach (var violation in violations)
        {
            Console.Error.WriteLine($"refused: {violation}");
        }

        return violations.Count > 0 ? 2 : await ExecuteAsync(args, dataset, runId, reports).ConfigureAwait(false);
    }

    private static async Task<int> ExecuteAsync(string[] args, RagEvaluationDataset dataset, string runId, string reports)
    {
        var options = Options(args);
        var started = DateTimeOffset.UtcNow;
        var watch = System.Diagnostics.Stopwatch.StartNew();

        using var tokenHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        ICallerTokenProvider tokens = new LocalProviderTokenProvider(options, tokenHttpClient, TimeProvider.System);

        if (!await PreflightAsync(options, tokens).ConfigureAwait(false))
        {
            return 2;
        }
        var runner = new EvaluationRunner(
            new AgentHttpClient(options.AgentEndpoint!, options.AgentName),
            new EvaluationTraceReader(options.TraceDirectory),
            tokens,
            options.CaseTimeout,
            options.MaximumConcurrentCases);
        var projector = new RowRunRagProjector(
            new EvaluationContextFactory(new WorkspaceTraceProjector(options.MaximumSelectedProductsPerComponent), SlotCapacities.Default));

        Console.WriteLine($"running {dataset.Cases.Count} cases from {dataset.Name} as {runId}...");
        var rows = await runner.RunAsync(Bridge(dataset), repetitions: 1, CancellationToken.None).ConfigureAwait(false);
        var results = rows.Select(projector.Project).ToArray();

        var thresholds = EvaluationThresholds.Load(Value(args, "--thresholds", DefaultThresholdsPath()));
        var record = ThresholdPolicy.Apply(new RunAssembler().Assemble(dataset, results, new RunIdentity(
            runId,
            "0.1.0",
            GeneratorModel(args),
            options.JudgeModel,
            started,
            watch.Elapsed.TotalMilliseconds), Imported(args)), thresholds);

        var path = await new JsonEvaluationRunStore(reports).SaveAsync(record).ConfigureAwait(false);
        Console.WriteLine($"stored {path}");
        Compare(args, record, reports);
        Console.WriteLine($"cases {record.TotalCases} correct {record.Correct} indeterminate {record.Indeterminate}");
        return 0;
    }

    private static Configuration.EvaluationOptions Options(string[] args)
        => new()
        {
            AgentEndpoint = new Uri(Value(args, "--agent-endpoint", "http://127.0.0.1:8088")),
            McpEndpoint = new Uri(Value(args, "--mcp-endpoint", "http://127.0.0.1:5199/mcp")),
            TraceDirectory = Value(args, "--trace-directory", "artifacts/evaluation/traces"),
            AuthMode = Configuration.EvaluationAuthMode.LocalProvider,
            MaximumConcurrentCases = 1,
            CaseTimeout = CaseTimeout(args),
        };

    private static EvaluationCaseSet Bridge(RagEvaluationDataset dataset)
        => new(
        [
            .. dataset.Cases.Select((testCase, index) => new EvaluationCase(
                RowNumber: index + 1,
                CaseId: testCase.Id,
                Sentence: testCase.Query,
                Variants: [new ExpectedSetupVariant([])],
                DuplicateGroup: null,
                AmbiguityGroup: testCase.ParaphraseGroupId)),
        ]);

    /// <summary>Imports the judge metrics a legacy run wrote, so one record carries every score.</summary>
    private static IReadOnlyList<EvaluationMetricResult> Imported(string[] args)
    {
        var path = Value(args, "--import-agent-eval", string.Empty);

        if (path.Length == 0 || !File.Exists(path))
        {
            return [];
        }

        var report = JsonSerializer.Deserialize<CoreRentalNet.Evaluation.Evaluation.Pipeline.EvaluationReport>(
            File.ReadAllText(path),
            EvaluationJson.Options);

        return report is null ? [] : AgentEvalMetricImport.Import(report.Metrics);
    }

    /// <summary>Prints the deltas against a baseline, after saying whether comparing them is fair.</summary>
    private static void Compare(string[] args, EvaluationRunRecord record, string reports)
    {
        var target = Value(args, "--compare-with", string.Empty);

        if (target.Length == 0)
        {
            return;
        }

        var baseline = new RunBaselineLocator(reports).Resolve(target, record.Dataset, record.RunId);

        if (baseline is null)
        {
            Console.WriteLine($"warning  no baseline found for '{target}'; nothing compared.");
            return;
        }

        var comparison = CoreRentalNet.Evaluation.Reporting.Comparison.RunComparer.Compare(baseline, record);
        foreach (var warning in comparison.Incompatibilities)
        {
            Console.WriteLine($"warning  {warning}");
        }

        foreach (var delta in comparison.Metrics)
        {
            Console.WriteLine($"{delta.Metric,-24} {Show(delta.Baseline),7} -> {Show(delta.Current),7}  delta {Show(delta.Delta)}");
        }
    }

    private static string Show(double? value) => value is null ? "n/a" : value.Value.ToString("0.000");

    /// <summary>Fails fast when the catalogue tools are unreachable, instead of reporting every case as a failure.</summary>
    private static async Task<bool> PreflightAsync(Configuration.EvaluationOptions options, ICallerTokenProvider tokens)
    {
        try
        {
            var token = await tokens.GetAsync(CancellationToken.None).ConfigureAwait(false);
            IToolCatalog catalogue = options.McpEndpoint is null
                ? new EmptyToolCatalog()
                : new McpToolCatalog(options.McpEndpoint);
            _ = await new AuthenticationPreflight(catalogue, TimeProvider.System)
                .RunAsync(token, CancellationToken.None)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is CallerTokenException or AuthenticationPreflightException)
        {
            Console.Error.WriteLine($"preflight failed: {exception.Message}");
            return false;
        }
    }

    /// <summary>Runs the whole dataset, or the subset a caller asked to smoke-test.</summary>
    private static RagEvaluationDataset Select(RagEvaluationDataset dataset, string[] args)
    {
        var caseId = Value(args, "--case", string.Empty);
        var limit = int.TryParse(Value(args, "--limit", string.Empty), out var parsed) ? parsed : int.MaxValue;

        var selected = dataset.Cases
            .Where(testCase => caseId.Length == 0 || string.Equals(testCase.Id, caseId, StringComparison.Ordinal))
            .Take(limit)
            .ToArray();

        return dataset with { Cases = selected };
    }

    /// <summary>A local run pays a cold model load inside its first case, so the budget defaults to ten minutes.</summary>
    private static TimeSpan CaseTimeout(string[] args)
        => int.TryParse(Value(args, "--case-timeout", "600"), out var seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.FromSeconds(600);

    private static string DefaultThresholdsPath()
        => Path.Combine(RepositoryRoot.Find(), "evaluation", "appsettings.json");

    /// <summary>The model under test, recorded separately from the judge so a report cannot conflate them.</summary>
    private static string GeneratorModel(string[] args)
        => Value(args, "--generator-model", "Qwen3-Coder-30B-A3B-Instruct");

    private static IReadOnlyList<string> PriorHashes(string reports)
        => Directory.Exists(reports)
            ? [.. Directory.EnumerateFiles(reports, "run.json", SearchOption.AllDirectories)
                .Select(path => JsonSerializer.Deserialize<EvaluationRunRecord>(File.ReadAllText(path), EvaluationJson.Options)?.DatasetSha256)
                .Where(hash => hash is not null)
                .Select(hash => hash!)]
            : [];

    private static void Report(RagEvaluationDataset dataset, DatasetValidation validation)
    {
        Console.WriteLine($"{dataset.Name}: {dataset.Cases.Count} cases, sha256 {dataset.Sha256[..12]}, excluded {dataset.Exclusions.Count}");
        foreach (var error in validation.Errors.Take(10))
        {
            Console.Error.WriteLine($"  error {error}");
        }
    }

    private static string Value(string[] args, string flag, string fallback)
    {
        var index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
