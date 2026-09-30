using System.ClientModel.Primitives;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>States the run's catalogue token on the invocation the client sends Foundry.</summary>
/// <remarks>
/// A pipeline policy rather than a header written at the call site: the agent is invoked by MAF, which builds the
/// request itself, so the token has to be stated at the one place every request passes through.
/// </remarks>
internal sealed class CallerAccessTokenInvocationPolicy : PipelinePolicy
{
    /// <inheritdoc />
    public override void Process(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        StateTheTokenOn(message);

        ProcessNext(message, pipeline, currentIndex);
    }

    /// <inheritdoc />
    public override async ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        StateTheTokenOn(message);

        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    private static void StateTheTokenOn(PipelineMessage message)
    {
        if (CallerAccessTokenForTheRunScope.TokenBeingCarried is { Length: > 0 } callerAccessToken)
        {
            message.Request.Headers.Set(
                MicrosoftFoundryAgentInvocationHeaders.CallerAccessToken,
                callerAccessToken);
        }
    }
}
