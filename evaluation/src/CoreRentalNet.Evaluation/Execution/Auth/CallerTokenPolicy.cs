using System.ClientModel.Primitives;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
namespace CoreRentalNet.Evaluation.Execution.Auth;

/// <summary>States the caller's catalogue token on every request to the agent host.</summary>
internal sealed class CallerTokenPolicy(string token) : PipelinePolicy
{
    private const string HeaderName = "x-client-mcp-catalog-access-token";

    public override void Process(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        message.Request.Headers.Set(HeaderName, token);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        message.Request.Headers.Set(HeaderName, token);
        await ProcessNextAsync(message, pipeline, currentIndex);
    }
}
