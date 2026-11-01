using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Context;
namespace CoreRentalNet.Evaluation.Evidence.Projection;

/// <summary>The single catalogue-to-slot translation used by the evaluator.</summary>
/// <remarks>
/// The agent has the same rule. It cannot be shared across the two processes, so this copy is pinned by a theory
/// test against the seven catalogue positions.
/// </remarks>
public static class EvaluationComponentMapping
{
    public static string SlotFor(string category, string? subCategory)
        => (category, subCategory) switch
        {
            ("desk", _) => EvaluationSlot.Desk,
            ("chair", _) => EvaluationSlot.Chair,
            ("accessory", "monitor") => EvaluationSlot.Monitor,
            ("accessory", "lamp") => EvaluationSlot.Lamp,
            ("accessory", "plant") => EvaluationSlot.Plant,
            ("accessory", "beanbag") => EvaluationSlot.RelaxZone,
            ("accessory", "coffee") => EvaluationSlot.CoffeeStation,
            _ => throw new ArgumentOutOfRangeException(
                nameof(category),
                (category, subCategory),
                "No workspace slot is searched for by those catalogue arguments."),
        };

    public static string SlotForComponent(string component)
        => component switch
        {
            "desk" => EvaluationSlot.Desk,
            "chair" => EvaluationSlot.Chair,
            "monitor" => EvaluationSlot.Monitor,
            "lamp" => EvaluationSlot.Lamp,
            "plant" => EvaluationSlot.Plant,
            "bean_bag" => EvaluationSlot.RelaxZone,
            "coffee_machine" => EvaluationSlot.CoffeeStation,
            _ => throw new ArgumentOutOfRangeException(
                nameof(component), component, "Unknown workspace component."),
        };
}
