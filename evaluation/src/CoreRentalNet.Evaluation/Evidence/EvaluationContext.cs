using System.Text.Json;
using CoreRentalNet.Evaluation.Cases;

namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>Everything one case's evaluator needs, in one structured value.</summary>
/// <remarks>
/// It is serialized into <c>EvalItem.Context</c>. The checks parse this one contract rather than re-reading the
/// event stream or the trace with their own rules.
/// </remarks>
public sealed record EvaluationContext(
    IReadOnlyList<ExpectedSetupVariant> ExpectedVariants,
    IReadOnlyDictionary<string, int> Capacities,
    IReadOnlyList<Candidate> Candidates,
    IReadOnlyList<string> Stages,
    string RunStatus,
    int FinalAttempt,
    bool CompletedIsLast,
    IReadOnlyList<ToolAnswerEvidence> ToolAnswers,
    IReadOnlyList<RerankSelectionEvidence> RerankSelections)
{
    public IReadOnlyList<ToolAnswerEvidence> FinalToolAnswers { get; } = ToolAnswers
        .Where(answer => answer.Attempt is null || answer.Attempt == FinalAttempt)
        .ToArray();

    public IReadOnlyList<RerankSelectionEvidence> FinalRerankSelections { get; } = RerankSelections
        .Where(selection => selection.Attempt is null || selection.Attempt == FinalAttempt)
        .ToArray();

    public IReadOnlySet<string> SelectedSkus { get; } = RerankSelections
        .Where(selection => selection.Attempt is null || selection.Attempt == FinalAttempt)
        .Select(selection => selection.Sku)
        .ToHashSet(StringComparer.Ordinal);

    public static EvaluationContext Parse(string? json)
        => JsonSerializer.Deserialize<EvaluationContext>(json ?? string.Empty, EvaluationJson.Options)
           ?? throw new InvalidOperationException("The evaluation context was not readable.");
}
