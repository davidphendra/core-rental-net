using System.Text.Json;
using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;

using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Context;
namespace CoreRentalNet.Evaluation.Evidence.Projection;

/// <summary>Turns raw evidence into the workspace facts the checks use.</summary>
public sealed class WorkspaceTraceProjector(int maximumSelectedProductsPerComponent = 3)
{
    private readonly int _maximumSelectedProductsPerComponent =
        maximumSelectedProductsPerComponent > 0
            ? maximumSelectedProductsPerComponent
            : throw new ArgumentOutOfRangeException(nameof(maximumSelectedProductsPerComponent));

    public WorkspaceEvidence Project(RunTrace trace)
        => new(
            ToolAnswers: ProjectToolAnswers(trace),
            RerankSelections: ProjectRerankSelections(trace));

    private IReadOnlyList<ToolAnswerEvidence> ProjectToolAnswers(RunTrace trace)
    {
        var answers = new List<ToolAnswerEvidence>();

        foreach (var exchange in trace.ToolExchanges
            .Where(exchange => WorkspaceToolNames.All.Contains(exchange.ToolName, StringComparer.Ordinal)))
        {
            var category = ToolArgumentText.Read(exchange.Arguments, "category");
            var subCategory = ToolArgumentText.Read(exchange.Arguments, "subCategory");

            if (category is null)
            {
                continue;
            }

            string slot;
            try
            {
                slot = EvaluationComponentMapping.SlotFor(category, subCategory);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            answers.Add(new ToolAnswerEvidence(
                Slot: slot,
                ToolName: exchange.ToolName,
                Arguments: exchange.Arguments,
                ReturnedSkus: ReturnedSkus(exchange.ResultText),
                Attempt: exchange.Attempt,
                ResultText: exchange.ResultText));
        }

        return answers;
    }

    private IReadOnlyList<RerankSelectionEvidence> ProjectRerankSelections(RunTrace trace)
    {
        var selections = new List<RerankSelectionEvidence>();

        foreach (var call in trace.StageCalls
            .Where(call => string.Equals(call.Stage, WorkspaceStageNames.Reranker, StringComparison.Ordinal)))
        {
            var ranking = TryReadRanking(call.ResponseText);

            if (ranking is null)
            {
                continue;
            }

            foreach (var (component, assessments) in ranking.Categories.EveryComponent())
            {
                string slot;
                try
                {
                    slot = EvaluationComponentMapping.SlotForComponent(component);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }

                var rank = 1;

                foreach (var assessment in assessments.Take(_maximumSelectedProductsPerComponent))
                {
                    selections.Add(new RerankSelectionEvidence(
                        Slot: slot,
                        Sku: assessment.Sku,
                        Rank: rank++,
                        Attempt: call.Attempt));
                }
            }
        }

        return selections;
    }

    private static IReadOnlyList<string> ReturnedSkus(string? resultText)
    {
        if (string.IsNullOrWhiteSpace(resultText))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<CatalogueAnswerDto>(resultText, EvaluationJson.Options)?
                .Matches?.Value
                .Select(item => item.Sku)
                .ToArray() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static RerankerRankingDto? TryReadRanking(string responseText)
    {
        try
        {
            return JsonSerializer.Deserialize<RerankerRankingDto>(responseText, EvaluationJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
