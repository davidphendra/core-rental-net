namespace CoreRentalNet.Evaluation.Cases;

/// <summary>Selects an evenly spaced, deterministic subset of the script.</summary>
/// <remarks>
/// Even spacing keeps a cheap run spread across the whole script instead of concentrating on its first few
/// rows. The same fixture and scope always select the same rows, so reports remain comparable.
/// </remarks>
public static class EvaluationCaseSetSelector
{
    /// <summary>The cases one scope selects, in their original order.</summary>
    public static EvaluationCaseSet Select(
        EvaluationCaseSet cases,
        EvaluationScope scope)
    {
        ArgumentNullException.ThrowIfNull(cases);

        var total = cases.Cases.Count;
        var target = Math.Min(TargetCount(scope, total), total);

        if (target == total)
        {
            return cases;
        }

        var selected = new EvaluationCase[target];

        for (var index = 0; index < target; index++)
        {
            selected[index] = cases.Cases[index * total / target];
        }

        return new EvaluationCaseSet(selected);
    }

    private static int TargetCount(EvaluationScope scope, int total)
        => scope switch
        {
            EvaluationScope.Tiny => 5,
            EvaluationScope.Small => 25,
            EvaluationScope.Half => (total + 1) / 2,
            EvaluationScope.All => total,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
        };
}
