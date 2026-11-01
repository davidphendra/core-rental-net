using System.Text.Json;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;

using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
namespace CoreRentalNet.Evaluation.Execution.Transport;

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
