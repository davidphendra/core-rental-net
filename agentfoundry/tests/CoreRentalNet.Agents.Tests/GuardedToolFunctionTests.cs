using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Guardrails.Arguments;
using CoreRentalNet.Agents.Shared.Guardrails.Pipeline;
using CoreRentalNet.Agents.Shared.Guardrails.Tools;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The caller token is ambient only while one catalogue tool is running.</summary>
public sealed class GuardedToolFunctionTests
{
    [Fact]
    public async Task The_tool_sees_the_caller_access_token()
    {
        var tokenValuesObservedByTool = new List<string?>();
        var catalogueTool = AIFunctionFactory.Create(
            () =>
            {
                tokenValuesObservedByTool.Add(McpRunScope.Current?.Token);
                return "tool-result";
            },
            "search_catalogue");

        var accessTokenService = new McpAccessTokenService(
            NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-access-token",
        };

        var guardedCatalogueTool = new GuardedToolFunction(
            catalogueTool,
            TestGuardrails.Pipeline,
            accessTokenService);

        var toolResult = await guardedCatalogueTool.InvokeAsync(
            new AIFunctionArguments());

        toolResult!.ToString().Should().Be("tool-result");
        tokenValuesObservedByTool.Should().Equal("the-callers-access-token");
        McpRunScope.Current.Should().BeNull(
            "the token must not remain ambient after the tool returns");
    }

    [Fact]
    public async Task The_scope_is_restored_when_the_tool_throws()
    {
        var catalogueTool = AIFunctionFactory.Create(
            ThrowingCatalogueTool,
            "search_catalogue");

        var accessTokenService = new McpAccessTokenService(
            NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-access-token",
        };

        var guardedCatalogueTool = new GuardedToolFunction(
            catalogueTool,
            TestGuardrails.Pipeline,
            accessTokenService);

        var toolCall = async () => await guardedCatalogueTool.InvokeAsync(
            new AIFunctionArguments());

        await toolCall.Should().ThrowAsync<InvalidOperationException>();
        McpRunScope.Current.Should().BeNull();
    }

    [Fact]
    public async Task The_authorization_guard_allows_a_call_inside_the_tool_scope()
    {
        var catalogueTool = AIFunctionFactory.Create(
            () => "tool-result",
            "search_catalogue");

        var accessTokenService = new McpAccessTokenService(
            NullLogger<McpAccessTokenService>.Instance)
        {
            Token = "the-callers-access-token",
        };

        var guardedCatalogueTool = new GuardedToolFunction(
            catalogueTool,
            new ToolGuardPipeline([new ToolAuthorizationGuard()], []),
            accessTokenService);

        var toolResult = await guardedCatalogueTool.InvokeAsync(
            new AIFunctionArguments());

        toolResult!.ToString().Should().Be("tool-result");
    }

    [Fact]
    public async Task The_authorization_guard_refuses_a_call_without_a_token()
    {
        var catalogueTool = AIFunctionFactory.Create(
            () => "tool-result",
            "search_catalogue");

        var guardedCatalogueTool = new GuardedToolFunction(
            catalogueTool,
            new ToolGuardPipeline([new ToolAuthorizationGuard()], []),
            new McpAccessTokenService(
                NullLogger<McpAccessTokenService>.Instance));

        var toolResult = await guardedCatalogueTool.InvokeAsync(
            new AIFunctionArguments());

        toolResult.Should().Be(
            "The call was refused: this run carries no caller token, so no tool was entitled to it.");
    }

    private static string ThrowingCatalogueTool()
        => throw new InvalidOperationException("tool failed");
}
