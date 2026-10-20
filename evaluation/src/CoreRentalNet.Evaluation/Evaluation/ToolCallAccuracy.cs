using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Checks that every requested component was searched with a valid catalogue call.</summary>
public static class ToolCallAccuracy
{
    public static ToolAccuracyResult Evaluate(EvaluationContext context)
    {
        var expectedSlots = context.ExpectedVariants[0].Slots;
        var answers = context.FinalToolAnswers;
        var answeredSlots = answers
            .Select(answer => answer.Slot)
            .ToHashSet(StringComparer.Ordinal);

        var coverage = expectedSlots.Count == 0
            ? 0
            : expectedSlots.Count(answeredSlots.Contains) / (double)expectedSlots.Count;

        var failures = new List<string>();

        failures.AddRange(expectedSlots
            .Where(slot => !answeredSlots.Contains(slot))
            .Select(FailureCodes.ToolNotCalled));

        failures.AddRange(answeredSlots
            .Where(slot => !expectedSlots.Contains(slot))
            .Select(FailureCodes.ToolUnexpectedCall));

        foreach (var answer in answers)
        {
            if (!HasValidArguments(answer))
            {
                failures.Add(FailureCodes.ToolInvalidArguments(answer.ToolName));
            }
        }

        var distinct = failures.Distinct(StringComparer.Ordinal).ToArray();

        return new ToolAccuracyResult(distinct.Length == 0, coverage, distinct);
    }

    private static bool HasValidArguments(ToolAnswerEvidence answer)
    {
        if (!WorkspaceToolNames.All.Contains(answer.ToolName, StringComparer.Ordinal))
        {
            return false;
        }

        if (ToolArgumentText.Read(answer.Arguments, "category") is not { Length: > 0 })
        {
            return false;
        }

        return string.Equals(answer.ToolName, WorkspaceToolNames.MeaningSearch, StringComparison.Ordinal)
            ? ToolArgumentText.Read(answer.Arguments, "query") is { Length: > 0 }
            : answer.Arguments.TryGetValue("terms", out var terms) && terms is not null;
    }
}
