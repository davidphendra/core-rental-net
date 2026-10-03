using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Mcp;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A catalogue search reaches the tool with the tool's own arguments, and the guardrail stays loud.</summary>
/// <remarks>
/// The regression: a monitor was handed over as <c>category: monitor</c> and the catalogue refused it, because the
/// input called the component "category" beside a "catalogCategory" the tool does not name. The tool's category is
/// the catalogue's - desk, chair or accessory - and the component travels as "component".
/// </remarks>
public sealed class CatalogueRetrievalArgumentsTests
{
    [Fact]
    public void A_monitor_search_carries_the_catalogue_category_and_its_subcategory()
    {
        var state = StateWithMonitorRelevant();

        var monitor = SearchFor(WorkspaceStageInput.Retrieval(state), "monitor");

        monitor.GetProperty("category").GetString().Should().Be("accessory");
        monitor.GetProperty("subCategory").GetString().Should().Be("monitor");
    }

    [Fact]
    public void A_desk_search_carries_no_subcategory()
    {
        var state = new WorkspaceSuggestionWorkflowState
        {
            CustomerWorkflowIdentifier = "run-1",
            OriginalCustomerQuery = "a desk and a chair",
            RequirementExpansion = WorkspaceRequirementExpansionFixtures.Valid(),
        };

        var desk = SearchFor(WorkspaceStageInput.Retrieval(state), "desk");

        desk.GetProperty("category").GetString().Should().Be("desk");
        desk.GetProperty("subCategory").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void The_component_is_named_separately_from_the_tool_category()
    {
        var state = StateWithMonitorRelevant();

        var monitor = SearchFor(WorkspaceStageInput.Retrieval(state), "monitor");

        monitor.GetProperty("component").GetString().Should().Be("monitor");
        monitor.GetProperty("category").GetString().Should().NotBe("monitor");
    }

    [Fact]
    public async Task The_guardrail_throws_a_dedicated_type_a_tool_failure_cannot_be_mistaken_for()
    {
        var accessTokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-own-token",
        };
        var guardrail = new GuardrailChatClient(
            new FixedChatClient("the token is the-callers-own-token"), accessTokens);

        var act = async () => await guardrail.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        await act.Should().ThrowAsync<CallerTokenLeakException>();
    }

    private static WorkspaceSuggestionWorkflowState StateWithMonitorRelevant()
        => new()
        {
            CustomerWorkflowIdentifier = "run-1",
            OriginalCustomerQuery = "a desk and a monitor",
            RequirementExpansion = WorkspaceRequirementExpansionFixtures.Deserialize(
                WorkspaceRequirementExpansionFixtures.Json(
                    monitorExpansion: WorkspaceRequirementExpansionFixtures.MonitorExpansion)),
        };

    private static JsonElement SearchFor(string retrievalInput, string component)
    {
        using var document = JsonDocument.Parse(retrievalInput);

        return document.RootElement.GetProperty("searches")
            .EnumerateArray()
            .Single(search => search.GetProperty("component").GetString() == component)
            .Clone();
    }
}
