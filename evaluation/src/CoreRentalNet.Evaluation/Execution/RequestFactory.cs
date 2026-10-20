using System.Text.Json;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Contracts;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Builds the application's request payload for one test case.</summary>
public static class RequestFactory
{
    public static string Create(EvaluationCase testCase, string runId)
    {
        var slots = SlotCapacities.Default.AsDictionary()
            .Select(pair => new SlotRuleDto(pair.Key, pair.Value))
            .ToArray();

        return JsonSerializer.Serialize(
            new SuggestionRequestDto(
                RunId: runId,
                Query: testCase.Sentence,
                Currency: "IDR",
                CeilingMonthly: null,
                Slots: slots),
            EvaluationJson.Options);
    }
}
