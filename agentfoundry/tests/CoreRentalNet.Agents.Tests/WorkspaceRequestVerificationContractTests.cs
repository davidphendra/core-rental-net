using AwesomeAssertions;
using Microsoft.Agents.AI;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using Xunit;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The verifier's contract: whether a sentence is a workspace request at all, as a typed verdict rather than an
/// error, and with no interpretation in it.
/// </summary>
/// <remarks>
/// This verdict used to belong to the rephraser. It moved because a run whose sentence is not about a workspace
/// must end before anything is read into it, and because the rephraser is asked again on every retry while the
/// verifier is asked exactly once.
/// </remarks>
public sealed class WorkspaceRequestVerificationContractTests
{
    [Fact]
    public async Task A_workspace_request_is_accepted_and_carries_no_reason()
    {
        var verdict = await Verdict("""{ "isWorkspaceRequest": true, "refusalReason": null }""");

        verdict.IsWorkspaceRequest.Should().BeTrue();
        verdict.RefusalReason.Should().BeNull();
    }

    [Fact]
    public async Task An_off_topic_sentence_is_refused_with_a_reason_rather_than_an_error()
    {
        var verdict = await Verdict(
            """{ "isWorkspaceRequest": false, "refusalReason": "not about furnishing a workspace" }""");

        verdict.IsWorkspaceRequest.Should().BeFalse();
        verdict.RefusalReason.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<WorkspaceRequestVerificationResult> Verdict(string reply)
    {
        var agent = AgentFactory.Build(WorkspaceSuggestionAgentRoster.Verifier, new FixedChatClient(reply));

        AgentResponse<WorkspaceRequestVerificationResult> response =
            await agent.RunAsync<WorkspaceRequestVerificationResult>("what is the weather today");

        return response.Result;
    }
}
