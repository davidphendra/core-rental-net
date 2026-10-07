using System.Diagnostics.Metrics;

namespace CoreRentalNet.BuildingBlocks.Application.Telemetry;

/// <summary>The business facts this application counts, named once and recorded where they happen.</summary>
/// <remarks>
/// <para>
/// <c>System.Diagnostics.Metrics</c> is the platform's rather than a provider's. A module records a measurement
/// and learns nothing about where it is exported, so no module gains an Azure dependency, the port rule is
/// untouched, and the destination stays a deployment decision. The host is what subscribes the meter.
/// </para>
/// <para>
/// <b>These are metrics and not spans because metrics are aggregated and never sampled.</b> A sampled trace
/// explains one failure; only an unsampled counter can say whether failures became more frequent, which is the
/// question a business actually asks.
/// </para>
/// <para>
/// <b>Ratios are deliberately absent.</b> Only the numerator and the denominator are recorded - orders and
/// conversions, approvals and runs - because a ratio cannot be re-aggregated: a weekly ratio taken from daily
/// ratios is not the weekly ratio. The division belongs in the query.
/// </para>
/// </remarks>
public static class BusinessTelemetry
{
    /// <summary>The meter the deployment subscribes to.</summary>
    public const string Name = "CoreRentalNet.Business";

    private static readonly Meter Meter = new(Name);

    /// <summary>One catalogue search a caller performed.</summary>
    public static readonly Counter<long> CatalogSearches = Meter.CreateCounter<long>(
        "catalog.search.performed",
        unit: "{search}",
        description: "Catalogue searches a caller performed.");

    /// <summary>How many products one catalogue search answered with.</summary>
    public static readonly Histogram<int> CatalogSearchResults = Meter.CreateHistogram<int>(
        "catalog.search.results",
        unit: "{product}",
        description: "Products one catalogue search answered with.");

    /// <summary>One suggestion run the panel asked for.</summary>
    public static readonly Counter<long> SuggestionRunsStarted = Meter.CreateCounter<long>(
        "suggestion.run.started",
        unit: "{run}",
        description: "Suggestion runs a customer started.");

    /// <summary>One suggestion run that ended, tagged with the verdict it ended on.</summary>
    /// <remarks>
    /// One instrument and five tags rather than five instruments, because the endings are values of the same
    /// fact. The tag is bounded by the verdict enum, which is what makes it safe as a dimension.
    /// </remarks>
    public static readonly Counter<long> SuggestionRunsEnded = Meter.CreateCounter<long>(
        "suggestion.run.ended",
        unit: "{run}",
        description: "Suggestion runs that ended, by verdict.");

    /// <summary>How long one suggestion run took.</summary>
    public static readonly Histogram<double> SuggestionRunDuration = Meter.CreateHistogram<double>(
        "suggestion.run.duration",
        unit: "s",
        description: "Wall-clock time one suggestion run took.");

    /// <summary>One suggestion a customer approved into the workspace.</summary>
    public static readonly Counter<long> SuggestionCandidatesApproved = Meter.CreateCounter<long>(
        "suggestion.candidate.approved",
        unit: "{suggestion}",
        description: "Suggestions a customer approved.");

    /// <summary>One order the application placed.</summary>
    public static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>(
        "order.placed",
        unit: "{order}",
        description: "Orders placed.");

    /// <summary>What one order billed up front.</summary>
    public static readonly Histogram<decimal> OrderFirstInvoiceValue = Meter.CreateHistogram<decimal>(
        "order.value.first_invoice",
        unit: "IDR",
        description: "What one order is billed up front.");

    /// <summary>The monthly value of one order.</summary>
    public static readonly Histogram<decimal> OrderMonthlyValue = Meter.CreateHistogram<decimal>(
        "order.value.monthly",
        unit: "IDR",
        description: "Monthly value of one order.");

    /// <summary>One conversion of a workspace into an order, tagged with whether it was the first.</summary>
    public static readonly Counter<long> WorkspaceConversions = Meter.CreateCounter<long>(
        "workspace.conversion",
        unit: "{conversion}",
        description: "Workspaces converted into orders.");
}
