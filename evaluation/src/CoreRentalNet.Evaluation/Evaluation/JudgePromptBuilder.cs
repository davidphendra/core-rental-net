using System.Reflection;
using System.Text.Json;
using CoreRentalNet.Evaluation.Contracts;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Builds the versioned judge prompt from one item's evidence.</summary>
public sealed class JudgePromptBuilder
{
    private const string PromptResourceName =
        "CoreRentalNet.Evaluation.Prompts.workspace-evaluation-judge.v2.md";

    private static readonly string SystemPrompt = ReadPrompt();

    public IReadOnlyList<ChatMessage> Build(EvaluationContext context, string request)
    {
        var products = CatalogueProducts(context);
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
                products = answer.ReturnedSkus
                    .Select(sku => ProductEvidence(answer.Slot, sku, products))
                    .ToArray(),
            }),
            afterReranker = context.FinalRerankSelections.Select(selection => new
            {
                slot = selection.Slot,
                sku = selection.Sku,
                rank = selection.Rank,
                product = ProductEvidence(selection.Slot, selection.Sku, products),
            }),
            finalSetup = context.Candidates.Select(candidate => new
            {
                lines = candidate.Lines.Select(line => new JudgeFinalLine(
                    line.Slot,
                    line.Sku,
                    line.Name,
                    line.Quantity,
                    line.Amount,
                    products.GetValueOrDefault(line.Sku)?.Description)),
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

    private static JudgeProductEvidence ProductEvidence(
        string slot,
        string sku,
        IReadOnlyDictionary<string, CatalogueItemDto> products)
    {
        var product = products.GetValueOrDefault(sku);

        return new JudgeProductEvidence(
            slot,
            sku,
            product?.Name ?? sku,
            product?.Description,
            product?.MonthlyPrice?.Amount);
    }

    private static IReadOnlyDictionary<string, CatalogueItemDto> CatalogueProducts(EvaluationContext context)
    {
        var products = new Dictionary<string, CatalogueItemDto>(StringComparer.Ordinal);

        foreach (var answer in context.FinalToolAnswers)
        {
            if (string.IsNullOrWhiteSpace(answer.ResultText))
            {
                continue;
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<CatalogueAnswerDto>(
                    answer.ResultText,
                    EvaluationJson.Options);

                foreach (var product in parsed?.Matches?.Value ?? [])
                {
                    products.TryAdd(product.Sku, product);
                }
            }
            catch (JsonException)
            {
                // A malformed search result is not evidence the judge can use.
            }
        }

        return products;
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
