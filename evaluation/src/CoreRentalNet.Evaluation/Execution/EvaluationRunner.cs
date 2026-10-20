using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Contracts;
using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Runs every case three times and pairs each execution with its own trace.</summary>
public sealed class EvaluationRunner(
    IAgentClient agentClient,
    ITraceReader traceReader)
{
    public async Task<IReadOnlyList<RowRun>> RunAsync(
        EvaluationCaseSet caseSet,
        int repetitions,
        CancellationToken cancellationToken)
    {
        var runs = new List<RowRun>(caseSet.Cases.Count * repetitions);

        foreach (var testCase in caseSet.Cases)
        {
            for (var repetition = 1; repetition <= repetitions; repetition++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var runId = $"eval-{testCase.CaseId}-r{repetition}";
                var request = RequestFactory.Create(testCase, runId);
                var events = new List<StreamEventDto>();

                await foreach (var json in agentClient.RunAsync(request, cancellationToken))
                {
                    events.Add(StreamEventReader.Read(json));
                }

                var trace = await traceReader.ReadAsync(runId, cancellationToken);

                runs.Add(new RowRun(testCase, repetition, runId, events, trace));
            }
        }

        return runs;
    }
}
