using System.Text.Json;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Contracts;
using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Evaluation.AgentEval;

/// <summary>Renders the catalogue evidence the RAG judge sees, without the expected answer.</summary>
public static class AgentEvalRetrievedEvidence
{
    public static string Render(EvaluationContext context)
    {
        var products = Products(context);
        var lines = new List<string>();

        foreach (var answer in context.FinalToolAnswers)
        {
            lines.Add($"[{answer.Slot}] {answer.ToolName} returned:");

            foreach (var sku in answer.ReturnedSkus)
            {
                lines.Add(Line(sku, products));
            }
        }

        foreach (var selection in context.FinalRerankSelections)
        {
            lines.Add($"[{selection.Slot}] selected rank {selection.Rank}:");
            lines.Add(Line(selection.Sku, products));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Line(string sku, IReadOnlyDictionary<string, CatalogueItemDto> products)
    {
        var product = products.GetValueOrDefault(sku);
        var name = product?.Name ?? sku;
        var description = product?.Description;
        var price = product?.MonthlyPrice is { } monthly
            ? $"{monthly.Amount:0.##} {monthly.Currency}"
            : null;

        var details = new[] { name, description, price }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return $"- {sku}: {string.Join(" | ", details)}";
    }

    private static IReadOnlyDictionary<string, CatalogueItemDto> Products(EvaluationContext context)
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
}
