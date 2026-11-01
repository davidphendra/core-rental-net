using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>How much of the test script one evaluation run covers.</summary>
public enum EvaluationScope
{
    Tiny,
    Small,
    Half,
    All,
}
