using System.ClientModel.Primitives;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>States the run's catalogue token on the invocation the client sends Foundry.</summary>
/// <remarks>
/// A pipeline policy rather than a header written at the call site: the agent is invoked by MAF, which builds the
/// request itself, so the token has to be stated at the one place every request passes through.
/// </remarks>
internal sealed class AccessTokenPipelinePolicy : PipelinePolicy
{
    /// <inheritdoc />
    public override void Process(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        StateAccessToken(message);

        ProcessNext(message, pipeline, currentIndex);
    }

    /// <inheritdoc />
    public override async ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        StateAccessToken(message);

        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    private void StateAccessToken(PipelineMessage message)
    {
        if (RunScopeAccessToken.Current is { AccessToken.Length: > 0 } runContext)
        {
            message.Request.Headers.Set(
                AgentFoundryInvocationHeaders.AccessToken,
                runContext.AccessToken);
        }
    }
}
