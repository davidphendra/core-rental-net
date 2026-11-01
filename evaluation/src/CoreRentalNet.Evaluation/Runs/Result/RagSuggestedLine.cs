using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;

namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>One product line in the workspace the agent recommended, with the quantity it claimed.</summary>
public sealed record RagSuggestedLine(string Slot, string Sku, int Quantity);
