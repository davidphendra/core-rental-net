using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>The three outcomes the agent may report, plus the one the harness adds when a run says nothing.</summary>
/// <remarks>
/// <see cref="Indeterminate"/> exists so a missing <c>WORKFLOW_STATUS</c> becomes a visible result rather than a
/// silent success. It is never a legal expectation and never counts toward outcome accuracy.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<RagOutcome>))]
public enum RagOutcome
{
    Success,
    Rejected,
    Unavailable,
    Indeterminate,
}
