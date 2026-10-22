using System.Runtime.CompilerServices;
using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The authorised client must not make the token ambient during a model call.</summary>
public sealed class AuthorisedMcpChatClientScopeTests
{
    [Fact]
    public async Task A_model_call_does_not_hold_the_caller_access_token_in_scope()
    {
        var innerClient = new ScopeCapturingChatClient();
        var authorisedClient = BuildAuthorisedClient(
            innerClient,
            callerAccessToken: "the-callers-access-token");

        await authorisedClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "a desk")]);

        innerClient.ScopeDuringModelCall.Should().BeNull();
        McpRunScope.Current.Should().BeNull();
    }

    [Fact]
    public async Task A_streamed_model_call_does_not_hold_the_caller_access_token_in_scope()
    {
        var innerClient = new ScopeCapturingChatClient();
        var authorisedClient = BuildAuthorisedClient(
            innerClient,
            callerAccessToken: "the-callers-access-token");

        await foreach (var _ in authorisedClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "a desk")]))
        {
        }

        innerClient.ScopeDuringModelCall.Should().BeNull();
        McpRunScope.Current.Should().BeNull();
    }

    private static AuthorisedMcpChatClient BuildAuthorisedClient(
        IChatClient innerClient,
        string callerAccessToken)
    {
        var accessTokenService = new McpAccessTokenService(
            NullLogger<McpAccessTokenService>.Instance);

        return new AuthorisedMcpChatClient(
            innerClient,
            new StubRunScopedServiceResolver(
                (typeof(IMcpAccessTokenService), accessTokenService),
                (typeof(IMcpAuthorizationConnection), new EmptyCatalogueConnection()),
                (typeof(IToolGuardPipeline), TestGuardrails.Pipeline),
                (typeof(AccessTokenHeaderReader), TheInvocationARunArrivesIn.Carrying(
                    AccessTokenHeader.Name,
                    callerAccessToken))),
            offersMcpTools: true,
            TestGuardrails.AllowList,
            NullLogger<AuthorisedMcpChatClient>.Instance);
    }

    private sealed class ScopeCapturingChatClient : IChatClient
    {
        public McpRunContext? ScopeDuringModelCall { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ScopeDuringModelCall = McpRunScope.Current;

            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "{}")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ScopeDuringModelCall = McpRunScope.Current;

            await Task.Yield();

            yield return new ChatResponseUpdate(ChatRole.Assistant, "{}");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class EmptyCatalogueConnection : IMcpAuthorizationConnection
    {
        public bool IsConfigured => true;

        public Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AITool>>([]);

        public ValueTask CloseAsync() => ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
