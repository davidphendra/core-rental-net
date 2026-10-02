using System.Net;
using System.Net.Http.Json;
using System.Text;
using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>
/// The frames a run produces when the agent has something to say: a stage, a retry, an approved setup and the
/// ending, each as its own server-sent event.
/// </summary>
/// <remarks>
/// A stand-in agent is supplied, so the run does not end unavailable and the framing of every frame kind the
/// browser reads can be asserted rather than assumed.
/// </remarks>
public sealed class SuggestionStreamingFrameTests
{
    private const string Permission = "builder:ai";

    private static readonly WorkspaceSuggestionCandidate ApprovedSetup = new(
        MonthlyTotal: 4_200_000m,
        Rationale: "A calm, focused setup.",
        Lines: [new WorkspaceSuggestionCandidateLine(SlotId.Desk, "DSKB08XN4JDR", "Sit-Stand Desk", 1, 4_200_000m)]);

    [Fact]
    public async Task A_stage_a_retry_an_approved_setup_and_the_ending_are_frames_of_their_own()
    {
        using var factory = new SuggestionEndpointFactory(new ScriptedSuggestionAgentAdapter(
            new WorkspaceSuggestionChangedEvent("verifyingRequest"),
            new WorkspaceSuggestionRetryEvent(2, 3),
            new WorkspaceSuggestionCandidateApprovedEvent(ApprovedSetup),
            new WorkspaceSuggestionResultReadyEvent(
                new WorkspaceSuggestionAnswer(WorkspaceSuggestionAnswerStatus.Suggested, [ApprovedSetup], null),
                "hash-1")));

        var frames = await RunAsync(factory);

        frames.Should().Contain("event: stage");
        frames.Should().Contain(WorkspaceSuggestionStage.Reading, "the agent's stage name is worded by the application");
        frames.Should().Contain("event: retry");
        frames.Should().Contain("attempt 2 of 3");
        frames.Should().Contain("event: candidate");
        frames.Should().Contain("event: result");
        frames.Should().Contain("DSKB08XN4JDR", "the setup the agent approved reaches the browser");
    }

    [Fact]
    public async Task The_ending_frame_is_the_last_thing_written()
    {
        using var factory = new SuggestionEndpointFactory(new ScriptedSuggestionAgentAdapter(
            new WorkspaceSuggestionCandidateApprovedEvent(ApprovedSetup),
            new WorkspaceSuggestionResultReadyEvent(
                new WorkspaceSuggestionAnswer(WorkspaceSuggestionAnswerStatus.Suggested, [ApprovedSetup], null),
                "hash-1")));

        var frames = await RunAsync(factory);

        frames.LastIndexOf("event: candidate", StringComparison.Ordinal)
            .Should().BeLessThan(
                frames.LastIndexOf("event: result", StringComparison.Ordinal),
                "a customer is shown the setups before the run says it is over");
    }

    private static async Task<string> RunAsync(SuggestionEndpointFactory factory)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuilderRoutes.Suggest)
        {
            Content = JsonContent.Create(new { query = "a desk and a chair, for a small room" }),
        };

        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, Permission);

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }
}
