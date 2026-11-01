using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
using Microsoft.Extensions.AI;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Execution;

public sealed class AuthenticationPreflightTests
{
    [Fact]
    public async Task A_valid_token_and_both_tools_pass_the_preflight()
    {
        var catalog = new FakeCatalog(
            Tool(WorkspaceToolNames.NameSearch),
            Tool(WorkspaceToolNames.MeaningSearch));

        var tools = await new AuthenticationPreflight(catalog, TimeProvider.System)
            .RunAsync(Token(DateTimeOffset.UtcNow.AddMinutes(5)), CancellationToken.None);

        tools.Should().HaveCount(2);
        catalog.CallerToken.Should().Be("token");
    }

    [Fact]
    public async Task An_expired_token_fails_before_the_catalogue_is_called()
    {
        var catalog = new FakeCatalog(Tool(WorkspaceToolNames.NameSearch));

        var act = () => new AuthenticationPreflight(catalog, TimeProvider.System)
            .RunAsync(Token(DateTimeOffset.UtcNow.AddMinutes(-1)), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationPreflightException>()
            .WithMessage("*expired*");

        catalog.Calls.Should().Be(0);
    }

    [Fact]
    public async Task A_missing_catalogue_tool_fails_the_preflight()
    {
        var catalog = new FakeCatalog(Tool(WorkspaceToolNames.NameSearch));

        var act = () => new AuthenticationPreflight(catalog, TimeProvider.System)
            .RunAsync(Token(DateTimeOffset.UtcNow.AddMinutes(5)), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationPreflightException>()
            .WithMessage($"*{WorkspaceToolNames.MeaningSearch}*");
    }

    [Fact]
    public async Task A_catalogue_refusal_is_reported_as_a_preflight_failure()
    {
        var catalog = new FakeCatalog(new InvalidOperationException("401 Unauthorized"));

        var act = () => new AuthenticationPreflight(catalog, TimeProvider.System)
            .RunAsync(Token(DateTimeOffset.UtcNow.AddMinutes(5)), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationPreflightException>()
            .WithMessage("*401*");
    }

    private static CallerToken Token(DateTimeOffset expiresAt)
        => new("token", expiresAt, null, [], []);

    private static AITool Tool(string name)
        => AIFunctionFactory.Create(() => "ok", name);

    private sealed class FakeCatalog : IToolCatalog
    {
        private readonly IReadOnlyList<AITool> _tools;
        private readonly Exception? _failure;

        public FakeCatalog(params AITool[] tools)
            => _tools = tools;

        public FakeCatalog(Exception failure)
        {
            _tools = [];
            _failure = failure;
        }

        public int Calls { get; private set; }

        public string? CallerToken { get; private set; }

        public Task<IReadOnlyList<AITool>> ListAsync(
            string callerToken,
            CancellationToken cancellationToken)
        {
            Calls++;
            CallerToken = callerToken;

            return _failure is null
                ? Task.FromResult(_tools)
                : Task.FromException<IReadOnlyList<AITool>>(_failure);
        }
    }
}
