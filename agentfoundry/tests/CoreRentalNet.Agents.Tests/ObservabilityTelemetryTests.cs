using System.Diagnostics.Metrics;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Telemetry;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The telemetry vocabulary and the run span: what a backend is told, and that one run is one span.</summary>
/// <remarks>
/// A listener observes the same instrument and source a real exporter would, so these prove the emission rather
/// than a substitute for it. Assertions match on a run's own identifier because the source and the meter are
/// process-wide and another test may be emitting at the same time.
/// </remarks>
public sealed class ObservabilityTelemetryTests
{
    [Fact]
    public void Every_instrument_is_named_under_the_workspace_prefix_and_only_once()
    {
        var instrumentNames = typeof(WorkspaceTelemetry)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => typeof(Instrument).IsAssignableFrom(field.FieldType))
            .Select(field => ((Instrument)field.GetValue(null)!).Name)
            .ToList();

        instrumentNames.Should().NotBeEmpty();
        instrumentNames.Should().OnlyContain(name => name.StartsWith("workspace.", StringComparison.Ordinal));
        instrumentNames.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void The_source_and_the_meter_share_one_name()
    {
        WorkspaceTelemetry.ActivitySource.Name.Should().Be(WorkspaceTelemetry.Name);
        WorkspaceTelemetry.Meter.Name.Should().Be(WorkspaceTelemetry.Name);
    }

    [Fact]
    public void A_run_is_span_and_counted_by_outcome()
    {
        using var recorder = new TelemetryRecorder();
        var telemetry = new TelemetryChatClient(
            new FixedChatClient("{}"), "gpt-4.1-mini", "test-prompts", NullLogger<TelemetryChatClient>.Instance);

        telemetry.StartRun("run-under-test");
        telemetry.CompleteRun("rejected");

        recorder.Spans.Should().Contain(span =>
            span.OperationName == WorkspaceTelemetry.RunSpanName
            && (string?)span.GetTagItem(WorkspaceTelemetry.RunId) == "run-under-test");

        recorder.Measurements.Should().Contain(measurement => measurement.Name == "workspace.run.count");
    }

    [Fact]
    public async Task The_guardrail_marks_the_span_it_stops()
    {
        using var recorder = new TelemetryRecorder();
        using var activity = WorkspaceTelemetry.ActivitySource.StartActivity("node-under-test");
        var accessTokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-own-token",
        };
        var guardrail = new GuardrailChatClient(
            new FixedChatClient("the token is the-callers-own-token"), accessTokens);

        var act = async () => await guardrail.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        await act.Should().ThrowAsync<InvalidOperationException>();
        ((bool)activity!.GetTagItem(WorkspaceTelemetry.TokenLeakDetected)!).Should().BeTrue();
        recorder.Measurements.Should().Contain(measurement =>
            measurement.Name == "workspace.guardrail.token_leak.count");
    }
}
