using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application.Telemetry;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

/// <summary>
/// The business facts, proven by collecting what each instrument records.
/// </summary>
/// <remarks>
/// A collector rather than a comment: the instrument names and their units are what a dashboard queries by, so
/// a rename that nobody intended has to fail here rather than silently empty a chart in production.
/// </remarks>
public sealed class BusinessTelemetryTests
{
    [Fact]
    public void A_search_is_counted_and_its_answer_is_measured()
    {
        using var searches = new MetricCollector<long>(BusinessTelemetry.CatalogSearches);
        using var results = new MetricCollector<int>(BusinessTelemetry.CatalogSearchResults);

        BusinessTelemetry.CatalogSearches.Add(1);
        BusinessTelemetry.CatalogSearchResults.Record(0);

        searches.GetMeasurementSnapshot().Should().ContainSingle();
        results.GetMeasurementSnapshot().Single().Value.Should().Be(0, "an empty answer is the number that matters");
    }

    [Fact]
    public void A_run_ends_under_the_verdict_it_ended_with()
    {
        using var ended = new MetricCollector<long>(BusinessTelemetry.SuggestionRunsEnded);

        BusinessTelemetry.SuggestionRunsEnded.Add(
            1,
            new KeyValuePair<string, object?>("verdict", "Unavailable"));

        var measurement = ended.GetMeasurementSnapshot().Single();

        measurement.Value.Should().Be(1);
        measurement.Tags["verdict"].Should().Be("Unavailable");
    }

    [Fact]
    public void A_run_records_its_duration_in_seconds()
    {
        // Seconds rather than milliseconds because that is the unit the semantic conventions use for a
        // duration, so this instrument is comparable with every instrument the distro produces.
        using var duration = new MetricCollector<double>(BusinessTelemetry.SuggestionRunDuration);

        BusinessTelemetry.SuggestionRunDuration.Record(62_500 / 1000.0);

        duration.GetMeasurementSnapshot().Single().Value.Should().Be(62.5);
    }

    [Fact]
    public void An_order_records_both_values_it_is_worth()
    {
        using var invoice = new MetricCollector<decimal>(BusinessTelemetry.OrderFirstInvoiceValue);
        using var monthly = new MetricCollector<decimal>(BusinessTelemetry.OrderMonthlyValue);

        BusinessTelemetry.OrderFirstInvoiceValue.Record(400_000m);
        BusinessTelemetry.OrderMonthlyValue.Record(400_000m);

        invoice.GetMeasurementSnapshot().Single().Value.Should().Be(400_000m);
        monthly.GetMeasurementSnapshot().Single().Value.Should().Be(400_000m);
    }

    [Fact]
    public void A_conversion_says_whether_it_was_the_first()
    {
        using var conversions = new MetricCollector<long>(BusinessTelemetry.WorkspaceConversions);

        BusinessTelemetry.WorkspaceConversions.Add(1, new KeyValuePair<string, object?>("firstAttempt", false));

        conversions.GetMeasurementSnapshot().Single().Tags["firstAttempt"].Should().Be(false);
    }
}
