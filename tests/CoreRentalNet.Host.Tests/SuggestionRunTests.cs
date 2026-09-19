using System.Text;
using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// One run, from the frames it writes to what is in them.
/// </summary>
/// <remarks>
/// The frames are read off a real <see cref="HttpResponse"/> writing into memory, so the framing is the
/// production framing and not a description of it. What is asserted is the two things the story is about:
/// the customer reads the model's <b>words</b> and never its JSON, and the result arrives <b>whole</b>.
/// </remarks>
public sealed class SuggestionRunTests
{
    private const string Answer =
        """
        { "status": "suggested", "reason": null,
          "options": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "quantity": 1, "why": "a stable surface" } ],
                         "rationale": "A calm, focused setup." } ] }
        """;

    [Fact] // AIWB-13, at the level where the leak would happen
    public async Task No_prose_frame_carries_raw_json_at_any_point_of_a_run()
    {
        var frames = await Run(scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer), Completed()]);

        // Only the frames the page renders as prose. The result frame is structured data the page parses
        // into candidates, and it is JSON on purpose; what must never happen is the document the model
        // wrote arriving anywhere a customer reads it verbatim.
        var prose = frames
            .Where(frame => frame.Event is "stage" or "text")
            .Select(frame => frame.Data)
            .ToArray();

        prose.Should().NotContain(
            data => data.Contains('{') || data.Contains('"') || data.Contains("sku"),
            "the customer is shown the model's words, never the document they arrived in");

        prose.Should().Contain("A calm, focused setup.");
        prose.Should().Contain("a stable surface");

        // And the document travels exactly once, where it is meant to, so this test cannot pass by the
        // answer being dropped on the floor.
        frames.Where(frame => frame.Data.Contains('{')).Should().ContainSingle()
            .Which.Event.Should().Be("result");
    }

    [Fact] // e05s04: hygiene runs on a complete value, which is why a split URL cannot escape
    public async Task A_url_split_across_two_fragments_never_reaches_a_frame()
    {
        // The value is split inside the URL, which is the case token-streaming would lose.
        var frames = await Run(scripted:
        [
            new AgentSuggestionEvent.NarrativeDelta("""{ "status": "suggested", "reason": null, "options": [ { "lines": [], "rationale": "See https://exa"""),
            new AgentSuggestionEvent.NarrativeDelta("""mple.invalid/very/long for more." } ] }"""),
            Completed(),
        ]);

        var prose = frames.Select(frame => frame.Data).ToArray();

        prose.Should().NotContain(data => data.Contains("example.invalid"), "the strip never runs on half a URL");
        prose.Should().Contain("See for more.", "the whole value was stripped once it closed, not half of it");
    }

    [Fact] // the stage lines are the application's, written as the phases happen
    public async Task The_stages_are_written_around_the_run_in_order()
    {
        var frames = await Run(scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer), Completed()]);

        frames
            .Where(frame => frame.Event == "stage")
            .Select(frame => frame.Data)
            .Should().Equal(
                "Reading your request",
                "Matching the catalogue",
                "Checking the suggestion");
    }

    [Fact] // the result is one frame, after the checking stage, and never partial
    public async Task The_result_arrives_whole_and_as_one_frame()
    {
        var frames = await Run(scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer), Completed()]);

        var results = frames.Where(frame => frame.Event == "result").ToArray();

        results.Should().ContainSingle("a candidate half-arrived is a candidate a customer could act on");

        // And it is the whole answer, not a prefix of it.
        results[0].Data.Should().Contain("suggested");
        results[0].Data.Should().Contain("A calm, focused setup.");
        results[0].Data.Should().NotContain("\n", "a newline would split the frame");

        frames[^1].Should().Be(results[0], "the result ends the run");
    }

    [Fact] // a failure is a code the browser words, and the agent's reason does not travel
    public async Task A_run_that_could_not_be_made_fails_with_a_stable_code_and_no_diagnosis()
    {
        var frames = await Run(scripted: [new AgentSuggestionEvent.Unavailable("no identity was found at /very/secret")]);

        var failed = frames.Should().ContainSingle(frame => frame.Event == "failed").Subject;

        failed.Data.Should().Be(SuggestionEventStream.Unavailable);
        failed.Data.Should().NotContain("secret", "a diagnostic belongs on the run record, not on the page");

        frames.Should().NotContain(frame => frame.Event == "result");
        frames.Where(frame => frame.Event == "stage").Select(frame => frame.Data)
            .Should().NotContain("Checking the suggestion", "nothing was checked");
    }

    [Fact]
    public async Task An_empty_run_writes_its_stages_and_stops()
    {
        // The agent yielding nothing at all: the stream must end rather than wait for an event that is not
        // coming, and the customer must have been told the run had started.
        var frames = await Run(scripted: []);

        frames.Should().NotBeEmpty();
        frames.Should().NotContain(frame => frame.Event == "result");
    }

    [Fact] // AIWB-27 and AIWB-28, at the level where they are decided
    public async Task A_stopped_run_writes_no_failure_and_keeps_what_it_already_said()
    {
        // The two halves of the rule. A stop is not an error, so no failure frame is written; and the text is
        // kept rather than retracted, because it carries no price and no product name and cannot mislead.
        var (frames, thrown) = await Attempt(
            scripted: [new AgentSuggestionEvent.NarrativeDelta(Answer)],
            then: new OperationCanceledException("the customer pressed stop"));

        thrown.Should().BeOfType<OperationCanceledException>(
            "the endpoint is what turns this into an ordinary end, and it must be able to tell");

        frames.Should().NotContain(frame => frame.Event == "failed", "stopping is not failing");
        frames.Should().NotContain(frame => frame.Event == "result", "a stopped run applies nothing");
        frames.Should().Contain(
            frame => frame.Event == "text" && frame.Data == "A calm, focused setup.",
            "the text is kept, never retracted mid-read");
    }

    [Fact] // AIWB-23 and AIWB-49: a dropped connection is the same signal, so it lands in the same place
    public async Task A_connection_that_has_gone_ends_the_run_the_same_way_a_stop_does()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new GoneStream();

        var stream = await SuggestionEventStream.BeginAsync(context.Response, CancellationToken.None);
        var run = new SuggestionRun(
            new ScriptedSuggestionAgent([new AgentSuggestionEvent.NarrativeDelta(Answer)]),
            new SuggestionRequestBuilder(new TestCatalogue().Add("DSKB08XN4JDR", 266_000m), new WorkspaceSlotSettings()),
            stream);

        var act = async () => await run.RunAsync(new RunRequest("a quiet corner", null), CancellationToken.None);

        // The socket being gone surfaces as the same cancellation a reload produces, which is why the endpoint
        // needs no second rule for it - and why the guard is released by the same `using`.
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>A body whose other end has gone: every write fails the way a dropped connection does.</summary>
    private sealed class GoneStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new OperationCanceledException("the connection is gone");
    }

    private static AgentSuggestionEvent Completed()
        => new AgentSuggestionEvent.Completed(
            new AgentSuggestionResult(
                AgentSuggestionStatus.Suggested,
                null,
                [new AgentSuggestionOption(
                    [new AgentSuggestionLine(
                        CoreRentalNet.Modules.Workspace.Domain.SlotId.Desk,
                        "DSKB08XN4JDR",
                        1,
                        "a stable surface")],
                    "A calm, focused setup.")]),
            "payload-hash");

    /// <summary>Runs one scripted run and reads the frames it wrote.</summary>
    private static async Task<IReadOnlyList<Frame>> Run(AgentSuggestionEvent[] scripted)
        => (await Attempt(scripted)).Frames;

    /// <summary>
    /// Runs one scripted run, and reports both what it wrote and anything it threw.
    /// </summary>
    /// <remarks>
    /// The frames are read even when the run threw, because that is the point of the stopped cases: what was
    /// written <em>before</em> the cancellation is what the customer is left reading.
    /// </remarks>
    private static async Task<(IReadOnlyList<Frame> Frames, Exception? Thrown)> Attempt(
        AgentSuggestionEvent[] scripted,
        Exception? then = null)
    {
        var context = new DefaultHttpContext();
        var written = new MemoryStream();
        context.Response.Body = written;

        Exception? thrown = null;

        try
        {
            var stream = await SuggestionEventStream.BeginAsync(context.Response, CancellationToken.None);
            var run = new SuggestionRun(
                new ScriptedSuggestionAgent(scripted, then),
                new SuggestionRequestBuilder(new TestCatalogue().Add("DSKB08XN4JDR", 266_000m), new WorkspaceSlotSettings()),
                stream);

            await run.RunAsync(new RunRequest("a quiet corner", null), CancellationToken.None);
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        return (Frames(Encoding.UTF8.GetString(written.ToArray())), thrown);
    }

    /// <summary>The frames in what was written: an event name and its one line of data, per blank line.</summary>
    private static IReadOnlyList<Frame> Frames(string body)
    {
        var frames = new List<Frame>();

        foreach (var block in body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var name = lines[0]["event: ".Length..];
            var data = lines[1]["data: ".Length..];

            frames.Add(new Frame(name, data));
        }

        return frames;
    }

    private sealed record Frame(string Event, string Data);
}
