using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Microsoft.Extensions.AI;

using CoreRentalNet.Evaluation.Evaluators.Judges;
using CoreRentalNet.Evaluation.Evaluators.Correctness;
namespace CoreRentalNet.Evaluation.Evaluators.Shared;

/// <summary>The conversation pair a Microsoft.Extensions.AI.Evaluation evaluator reads.</summary>
/// <remarks>
/// The judge is shown the request and the rendered recommendation only. Retrieved products are not attached here:
/// groundedness is measured against them separately, so a judge cannot reward an answer for repeating its own
/// context back.
/// </remarks>
public sealed record JudgeInput(IReadOnlyList<ChatMessage> Messages, ChatResponse Response)
{
    public static JudgeInput From(RagRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new JudgeInput(
            Messages: [new ChatMessage(ChatRole.User, run.Query)],
            Response: new ChatResponse(new ChatMessage(ChatRole.Assistant, run.AnswerText)));
    }
}
