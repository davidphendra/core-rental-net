using System.Text.Json;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Tests;

/// <summary>Small, explicit contexts for the check tests.</summary>
internal static class ContextFixture
{
    public static ExpectedSetupVariant DeskAndChair { get; } = new(
    [
        new ExpectedLine(EvaluationSlot.Desk, ["D1"]),
        new ExpectedLine(EvaluationSlot.Chair, ["C1"]),
    ]);

    public static Candidate ValidCandidate { get; } = new(
    [
        new CandidateLine(EvaluationSlot.Desk, "D1", "Desk", 1, 100),
        new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
    ]);

    public static IReadOnlyDictionary<string, ProductRecord> Catalogue { get; } =
        new Dictionary<string, ProductRecord>(StringComparer.Ordinal)
        {
            ["D1"] = new("D1", "Desk", "desk", null, 100),
            ["C1"] = new("C1", "Chair", "chair", null, 100),
            ["M1"] = new("M1", "Monitor", "accessory", "monitor", 100),
        };

    public static EvaluationContext Create(
        IReadOnlyList<Candidate>? candidates = null,
        IReadOnlyList<ToolAnswerEvidence>? toolAnswers = null,
        IReadOnlyList<RerankSelectionEvidence>? rerankSelections = null,
        IReadOnlyList<string>? stages = null,
        string runStatus = "success",
        bool completedIsLast = true,
        int finalAttempt = 1,
        IReadOnlyList<ExpectedSetupVariant>? expectedVariants = null)
        => new(
            ExpectedVariants: expectedVariants ?? [DeskAndChair],
            Capacities: SlotCapacities.Default.AsDictionary(),
            Candidates: candidates ?? [ValidCandidate],
            Stages: stages ?? SuccessStages(),
            RunStatus: runStatus,
            FinalAttempt: finalAttempt,
            CompletedIsLast: completedIsLast,
            ToolAnswers: toolAnswers ?? DefaultToolAnswers(),
            RerankSelections: rerankSelections ?? DefaultSelections());

    public static EvalItem Item(EvaluationContext context)
        => new("a desk and a chair", "the candidate")
        {
            Context = JsonSerializer.Serialize(context, EvaluationJson.Options),
        };

    public static IReadOnlyList<string> SuccessStages()
        =>
        [
            "verifyingRequest",
            "rephrasingRequirement",
            "retrievingCatalogueProducts",
            "rerankingWorkspaceCandidates",
            "composingWorkspaceSetups",
        ];

    public static IReadOnlyList<ToolAnswerEvidence> DefaultToolAnswers()
        =>
        [
            ToolAnswer(EvaluationSlot.Desk, "D1"),
            ToolAnswer(EvaluationSlot.Chair, "C1"),
        ];

    public static ToolAnswerEvidence ToolAnswer(string slot, params string[] skus)
        => new(
            Slot: slot,
            ToolName: WorkspaceToolNames.NameSearch,
            Arguments: new Dictionary<string, object?>
            {
                ["category"] = slot is EvaluationSlot.Desk or EvaluationSlot.Chair ? slot.ToLowerInvariant() : "accessory",
                ["terms"] = "the need",
            },
            ReturnedSkus: skus,
            Attempt: null,
            ResultText: "{}");

    public static IReadOnlyList<RerankSelectionEvidence> DefaultSelections()
        => [new RerankSelectionEvidence(EvaluationSlot.Desk, "D1", 1, null)];
}
