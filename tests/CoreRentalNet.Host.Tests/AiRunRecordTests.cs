using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-39: the run record. A paid, quality-sensitive call has to be explainable after the fact - which
/// prompt, which model, how many calls, how much it cost, and what the customer asked.
/// </summary>
/// <remarks>
/// <para>
/// The assertions are on the LINE, not on the object: what a log sink receives is the JSON, so parsing it here
/// is what proves the record survives the trip rather than that a record can round-trip through itself.
/// </para>
/// <para>
/// Where it is written is a decision the epic does not make - the story asks for "one structured record per
/// run" with "bounded retention of 90 days", and the application has no store for it. It is written as a
/// structured line under a category of its own, so the retention is the deployment's log policy. Recorded in
/// the capsule rather than hidden here.
/// </para>
/// </remarks>
public sealed class AiRunRecordTests
{
    private const string Answer =
        """
        { "status": "suggested", "reason": null,
          "options": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "quantity": 1, "why": "a stable surface" } ],
                         "rationale": "A calm, focused setup." } ] }
        """;

    [Fact] // AIWB-39
    public async Task A_run_is_recorded_with_what_it_cost_and_what_was_asked()
    {
        var line = await Record(scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer), Completed()]);

        Text(line, "runId").Should().NotBeNullOrWhiteSpace("a run is joined to its application by this id");
        Text(line, "query").Should().Be("a quiet corner");
        Text(line, "payloadHash").Should().Be("payload-hash", "a hash of the projection actually sent");
        Text(line, "model").Should().Be("gpt-4.1-mini");
        Text(line, "promptVersion").Should().Be("rephraser.v1+suggestor.v1");

        // The field that makes the two-call assumption visible if it ever drifts, which is why it exists.
        line.GetProperty("modelCalls").GetInt32().Should().Be(2);
        line.GetProperty("inputTokens").GetInt32().Should().Be(172_000);
        line.GetProperty("outputTokens").GetInt32().Should().Be(400);
        line.GetProperty("latencyMilliseconds").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        Text(line, "verdict").Should().Be("suggested", "a word a person can read, not the number an enum defaults to");
    }

    [Fact] // the raw output is the point: a rejected run is otherwise unexplainable
    public async Task The_model_s_own_answer_is_kept_as_it_arrived()
    {
        var line = await Record(scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer), Completed()]);

        Text(line, "rawOutput").Should().Be(
            Answer,
            "the model's output rather than what the application salvaged - which is also what the evaluation tier grades");
    }

    [Fact] // AIWB-39's one prohibition
    public async Task Nothing_on_the_record_identifies_the_customer()
    {
        var line = await Record(scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer), Completed()]);

        // The record names a run by a hash of what the application already identifies the account by, so two
        // runs can be seen to be the same account's without the record holding an account.
        Text(line, "customerId").Should().Be("opaque-customer-id");

        var written = line.GetRawText();

        foreach (var pii in new[] { "Dewi", "dewi@example.com", "Villa Lotus", "auth0|" })
        {
            written.Should().NotContain(pii, "no name, no email and no address is on a run record");
        }
    }

    [Fact] // a run that could not be made is still recorded, or it is the one nobody can explain
    public async Task A_run_that_could_not_be_made_is_recorded_as_unavailable()
    {
        var line = await Record(scripted: [new AgentSuggestionEvent.Unavailable("no identity")]);

        Text(line, "verdict").Should().Be("unavailable");
        line.GetProperty("modelCalls").GetInt32().Should().Be(0, "nothing was spent");
        Text(line, "rawOutput").Should().BeEmpty();
    }

    [Fact] // and so is a stopped one, which is not a failure and must not read like one
    public async Task A_stopped_run_is_recorded_as_stopped_rather_than_as_a_failure()
    {
        var line = await Record(
            scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer)],
            then: new OperationCanceledException("the customer pressed stop"));

        Text(line, "verdict").Should().Be("stopped");
        Text(line, "rawOutput").Should().Be(Answer, "what it had already said is kept");
    }

    [Fact] // an answer that does not survive being checked is recorded as invalid, not as a suggestion
    public async Task A_run_whose_answer_fails_validation_is_recorded_as_invalid()
    {
        var line = await Record(
            scripted:
            [
                new AgentSuggestionEvent.NarrativeDelta(Answer),
                new AgentSuggestionEvent.Completed(
                    new AgentSuggestionResult(
                        AgentSuggestionStatus.Suggested,
                        null,
                        [new AgentSuggestionOption([new AgentSuggestionLine(SlotId.Desk, "NOT-A-SKU", 1, "why")], "r")],
                        new AgentRunUsage(2, 1, 1, "gpt-4.1-mini", "v")),
                    "payload-hash"),
            ]);

        Text(line, "verdict").Should().Be("invalid");
        Text(line, "rawOutput").Should().Be(Answer, "the answer is kept even when the application refuses it");
    }

    /// <summary>Runs one scripted run against a logger a test can read, and returns the line it wrote.</summary>
    private static async Task<JsonElement> Record(AgentSuggestionEvent[] scripted, Exception? then = null)
    {
        var logs = new CapturingLoggerProvider();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var catalogue = new TestCatalogue().Add("DSKB08XN4JDR", 266_000m);
        var slots = new WorkspaceSlotSettings();

        var stream = await SuggestionEventStream.BeginAsync(context.Response, CancellationToken.None);
        var run = new SuggestionRun(
            new ScriptedSuggestionAgent(scripted, then),
            new SuggestionRequestBuilder(catalogue, slots),
            new SuggestionValidator(catalogue, slots, new SuggestionSpread(SuggestionSpread.DefaultFactor)),
            stream,
            LoggerFactory.Create(builder => builder.AddProvider(logs)));

        var act = async () => await run.RunAsync(
            new RunRequest("a quiet corner", null),
            "opaque-customer-id",
            CancellationToken.None);

        if (then is null)
        {
            await act.Should().NotThrowAsync();
        }
        else
        {
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        var entry = logs.Entries.Single(captured => captured.Category == AiRunLog.Category);
        var json = entry.Property("Record") as string;

        json.Should().NotBeNull("the record travels as a structured property, so a sink can index its fields");

        return JsonDocument.Parse(json!).RootElement.Clone();
    }

    private static string? Text(JsonElement line, string name) => line.GetProperty(name).GetString();

    private static AgentSuggestionEvent Completed()
        => new AgentSuggestionEvent.Completed(
            new AgentSuggestionResult(
                AgentSuggestionStatus.Suggested,
                null,
                [new AgentSuggestionOption(
                    [new AgentSuggestionLine(SlotId.Desk, "DSKB08XN4JDR", 1, "a stable surface")],
                    "A calm, focused setup.")],
                new AgentRunUsage(2, 172_000, 400, "gpt-4.1-mini", "rephraser.v1+suggestor.v1")),
            "payload-hash");
}
