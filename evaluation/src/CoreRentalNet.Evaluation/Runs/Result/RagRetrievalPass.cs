using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>One retrieval tool's answer for one slot, kept so the two retrievers can be compared.</summary>
public sealed record RagRetrievalPass(string ToolName, IReadOnlyList<string> Skus);
