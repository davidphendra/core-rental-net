using System.Reflection;
using System.Text.Json;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Builds the versioned judge prompt from one item's evidence.</summary>
public sealed class JudgePromptBuilder
{
    private const string PromptResourceName =
        "CoreRentalNet.Evaluation.Prompts.workspace-evaluation-judge.v1.md";

    private static readonly string SystemPrompt = ReadPrompt();

    public IReadOnlyList<ChatMessage> Build(EvaluationContext context, string request)
    {
        var evidence = new
        {
            request,
            expectedSetup = context.ExpectedVariants.Select(variant => new
            {
                lines = variant.Lines.Select(line => new
                {
                    slot = line.Slot,
                    skus = line.Skus,
                }),
            }),
            beforeReranker = context.FinalToolAnswers.Select(answer => new
            {
                slot = answer.Slot,
                toolName = answer.ToolName,
                returnedSkus = answer.ReturnedSkus,
            }),
            afterReranker = context.FinalRerankSelections.Select(selection => new
            {
                slot = selection.Slot,
                sku = selection.Sku,
                rank = selection.Rank,
            }),
            finalSetup = context.Candidates.Select(candidate => new
            {
                lines = candidate.Lines.Select(line => new
                {
                    slot = line.Slot,
                    sku = line.Sku,
                    name = line.Name,
                }),
            }),
            deterministicFindings = new
            {
                answerExact = AnswerComparison.Evaluate(context).Exact,
                rerankerAligned = RerankerAlignment.Evaluate(context).Aligned,
                rawRetrievalRecall = context.FinalToolAnswers.Count > 0,
            },
            mismatch = RerankerMismatchDetector.Detect(context),
        };

        return
        [
            new ChatMessage(ChatRole.System, SystemPrompt),
            new ChatMessage(ChatRole.User, JsonSerializer.Serialize(evidence, EvaluationJson.Options)),
        ];
    }

    private static string ReadPrompt()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(PromptResourceName)
            ?? throw new InvalidOperationException($"Judge prompt resource '{PromptResourceName}' was not found.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
