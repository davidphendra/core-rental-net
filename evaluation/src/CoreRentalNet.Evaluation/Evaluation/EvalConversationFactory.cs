using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Builds the synthetic conversation a built-in evaluator reads.</summary>
public static class EvalConversationFactory
{
    public static IReadOnlyList<ChatMessage> Create(
        EvaluationContext context,
        string sentence)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, sentence),
        };

        var callIndex = 0;

        foreach (var answer in context.FinalToolAnswers)
        {
            var callId = $"eval-call-{++callIndex}";

            messages.Add(new ChatMessage(
                ChatRole.Assistant,
                [new FunctionCallContent(
                    callId,
                    answer.ToolName,
                    new Dictionary<string, object?>(answer.Arguments))]));

            messages.Add(new ChatMessage(
                ChatRole.Tool,
                [new FunctionResultContent(callId, answer.ResultText)]));
        }

        messages.Add(new ChatMessage(
            ChatRole.Assistant,
            EvaluationText.Candidate(context.Candidates)));

        return messages;
    }
}
