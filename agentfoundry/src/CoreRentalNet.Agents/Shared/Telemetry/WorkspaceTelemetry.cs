using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CoreRentalNet.Agents.Shared.Telemetry;

/// <summary>The one ActivitySource and one Meter this deployable emits under, and the names of what it emits.</summary>
/// <remarks>
/// <para>
/// One file and one vocabulary: a second source or meter would be a second thing a backend has to be told about,
/// and every stage shares these names. The instruments are immutable static readonly - the no-mutable-static-state
/// rule forbids working values in fields, and an instrument is safely shared by every caller.
/// </para>
/// <para>
/// The tag names live here rather than in a companion class because the repo's one-type-per-file rule would make a
/// companion a second file for no benefit, and because a name that is a constant is a typo the compiler catches
/// rather than a series nobody sees.
/// </para>
/// </remarks>
internal static class WorkspaceTelemetry
{
    /// <summary>The name of both the source and the meter, which is the name every backend is told.</summary>
    public const string Name = "CoreRentalNet.Agents";

    /// <summary>The span a run's stages nest under. It is closed by whichever node ends the run.</summary>
    public const string RunSpanName = "workspace.suggestion.run";

    /// <summary>
    /// Whether message content is attached to spans. Read from configuration and defaulted on, because the
    /// decision to capture content is temporary and must be reversible without recompiling - and because a
    /// hard-coded true is a decision nobody can revisit.
    /// </summary>
    public const string CaptureContentConfigurationKey = "Telemetry:CaptureContent";

    // Tag and dimension names.
    public const string RunId = "run.id";
    public const string RunStatus = "run.status";
    public const string RunAttempts = "run.attempts";
    public const string PromptVersion = "run.prompt.version";
    public const string NodeKind = "node.kind";
    public const string NodeStatus = "node.status";
    public const string Stage = "stage";
    public const string Component = "component";
    public const string ToolName = "tool.name";
    public const string Reason = "reason";
    public const string IssueCode = "issue.code";
    public const string Outcome = "outcome";
    public const string ErrorType = "error.type";

    /// <summary>The tag the guardrail sets on the span it stopped, for the redaction processor to find.</summary>
    public const string TokenLeakDetected = "guardrail.token_leak.detected";

    // The two node kinds, so the deterministic/model split is data rather than an inference.
    public const string NodeKindModel = "model";
    public const string NodeKindDeterministic = "deterministic";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);

    // Run and outcome.
    public static readonly Counter<long> RunCount =
        Meter.CreateCounter<long>("workspace.run.count", "{run}", "Runs by terminal outcome");
    public static readonly Histogram<int> RunAttemptCount =
        Meter.CreateHistogram<int>("workspace.run.attempts", "{attempt}", "Attempts a run consumed");
    public static readonly Counter<long> RunFailureReason =
        Meter.CreateCounter<long>("workspace.run.failure_reason.count", "{run}", "Why a run ended");
    public static readonly UpDownCounter<long> RunsInFlight =
        Meter.CreateUpDownCounter<long>("workspace.runs.in_flight", "{run}", "Runs started and not yet ended");
    public static readonly Counter<long> GuardrailTokenLeak =
        Meter.CreateCounter<long>("workspace.guardrail.token_leak.count", "{incident}", "Token-leak stops; expected 0");

    // Nodes.
    public static readonly Histogram<double> NodeDuration =
        Meter.CreateHistogram<double>("workspace.node.duration", "ms", "Wall time of one node");
    public static readonly Counter<long> NodeCount =
        Meter.CreateCounter<long>("workspace.node.count", "{node}", "Nodes by status");

    // Retry.
    public static readonly Counter<long> Retry =
        Meter.CreateCounter<long>("workspace.retry.count", "{retry}", "Retries by attempt-level reason");
    public static readonly Counter<long> RetryOutcome =
        Meter.CreateCounter<long>("workspace.retry.outcome.count", "{retry}", "How an attempt ended");

    // Model.
    public static readonly Histogram<double> ModelTimeToFirstToken =
        Meter.CreateHistogram<double>("workspace.model.time_to_first_token", "ms", "Time to the first streamed token");
    public static readonly Histogram<double> ModelTimePerOutputToken =
        Meter.CreateHistogram<double>("workspace.model.time_per_output_token", "ms", "Time between streamed tokens");
    public static readonly Counter<long> ModelContractViolation =
        Meter.CreateCounter<long>("workspace.model.contract_violation.count", "{violation}", "Model answers that failed their contract");
    public static readonly Counter<long> ModelThrottled =
        Meter.CreateCounter<long>("workspace.model.throttled.count", "{response}", "Model answers refused for rate");
    public static readonly Counter<long> ModelTransportRetry =
        Meter.CreateCounter<long>("workspace.model.transport_retry.count", "{retry}", "Transport retries of one model call");
    public static readonly Counter<long> ModelTimeout =
        Meter.CreateCounter<long>("workspace.model.timeout.count", "{timeout}", "Model calls that timed out");
    public static readonly Counter<long> ModelError =
        Meter.CreateCounter<long>("workspace.model.error.count", "{error}", "Model calls that failed");

    // Catalogue and retrieval.
    public static readonly Counter<long> CatalogueSearch =
        Meter.CreateCounter<long>("workspace.catalogue.search.count", "{search}", "Searches by tool and outcome");
    public static readonly Histogram<int> ProductsRetrieved =
        Meter.CreateHistogram<int>("workspace.catalogue.products.retrieved", "{product}", "Products a search returned");
    public static readonly Counter<long> ZeroResult =
        Meter.CreateCounter<long>("workspace.catalogue.zero_result.count", "{search}", "Searches that returned nothing");
    public static readonly Counter<long> ExcludedByCeiling =
        Meter.CreateCounter<long>("workspace.catalogue.excluded_by_ceiling.count", "{product}", "Products a ceiling excluded");
    public static readonly Histogram<int> DistinctProducts =
        Meter.CreateHistogram<int>("workspace.catalogue.distinct_products.count", "{product}", "Distinct products a run saw");
    public static readonly Histogram<double> CatalogueConnectionDuration =
        Meter.CreateHistogram<double>("workspace.catalogue.connection.duration", "ms", "Time to reach the catalogue");
    public static readonly Counter<long> CatalogueConnectionFailure =
        Meter.CreateCounter<long>("workspace.catalogue.connection.failure.count", "{failure}", "Catalogue connections that failed");
    public static readonly Counter<long> CatalogueToolTimeout =
        Meter.CreateCounter<long>("workspace.catalogue.tool.timeout.count", "{timeout}", "Catalogue tool calls that timed out");
    public static readonly Counter<long> CatalogueAuthFailure =
        Meter.CreateCounter<long>("workspace.catalogue.auth_failure.count", "{failure}", "Catalogue refusals of the caller's token");

    // Composition, review and budget.
    public static readonly Counter<long> RerankRelevance =
        Meter.CreateCounter<long>("workspace.rerank.relevance.count", "{product}", "Reranked products by relevance");
    public static readonly Histogram<int> CandidateCount =
        Meter.CreateHistogram<int>("workspace.candidate.count", "{setup}", "Setups a run proposed");
    public static readonly Counter<long> SetupApproved =
        Meter.CreateCounter<long>("workspace.setup.approved.count", "{setup}", "Setups a run approved");
    public static readonly Histogram<double> SetupMonthlyTotal =
        Meter.CreateHistogram<double>("workspace.setup.monthly_total", "{currency}", "Monthly total of an approved setup");
    public static readonly Histogram<int> SetupLines =
        Meter.CreateHistogram<int>("workspace.setup.lines", "{line}", "Lines in an approved setup");
    public static readonly Counter<long> ValidatorRejection =
        Meter.CreateCounter<long>("workspace.validator.rejection.count", "{rejection}", "Structural rejections by reason");
    public static readonly Histogram<int> ReviewIssues =
        Meter.CreateHistogram<int>("workspace.review.issues.count", "{issue}", "Review issues per reviewed attempt");
    public static readonly Counter<long> ReviewIssue =
        Meter.CreateCounter<long>("workspace.review.issue.count", "{issue}", "Review issues by bounded code");
    public static readonly Histogram<double> BudgetHeadroom =
        Meter.CreateHistogram<double>("workspace.budget.headroom", "{currency}", "Ceiling minus an approved total");
    public static readonly Counter<long> BudgetDerived =
        Meter.CreateCounter<long>("workspace.budget.derived.count", "{budget}", "Budgets the run derived");
    public static readonly Counter<long> BudgetExplicit =
        Meter.CreateCounter<long>("workspace.budget.explicit.count", "{budget}", "Budgets the customer stated");
    public static readonly Histogram<double> ComponentFilledRatio =
        Meter.CreateHistogram<double>("workspace.request.component_filled_ratio", "1", "Requested components an approved setup answered");
    public static readonly Counter<long> ComponentShortfall =
        Meter.CreateCounter<long>("workspace.component.shortfall.count", "{component}", "Requested components left unfilled");

    // Cost, stream and self-observation.
    public static readonly Histogram<double> CostEstimated =
        Meter.CreateHistogram<double>("workspace.cost.estimated", "{currency}", "Estimated cost of a run");
    public static readonly Histogram<double> StreamFirstEvent =
        Meter.CreateHistogram<double>("workspace.stream.first_event", "ms", "Time to the first streamed event");
    public static readonly Histogram<double> StreamGap =
        Meter.CreateHistogram<double>("workspace.stream.gap", "ms", "Gap between streamed events");
    public static readonly Counter<long> StreamAbandoned =
        Meter.CreateCounter<long>("workspace.stream.abandoned.count", "{run}", "Runs the caller stopped reading");
    public static readonly Counter<long> WorkflowCheckpointResume =
        Meter.CreateCounter<long>("workspace.workflow.checkpoint_resume.count", "{resume}", "Workflow resumes by result");
    public static readonly Counter<long> TelemetryRedaction =
        Meter.CreateCounter<long>("workspace.telemetry.redaction.count", "{span}", "Spans scrubbed before export");
    public static readonly Counter<long> TelemetryExporterFailure =
        Meter.CreateCounter<long>("workspace.telemetry.exporter_failure.count", "{failure}", "Telemetry exports that failed");
}
