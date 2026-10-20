using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One component's ranked products.</summary>
public sealed record RerankerAssessmentsDto(
    [property: JsonPropertyName("desk")] IReadOnlyList<RerankerAssessmentDto>? Desk,
    [property: JsonPropertyName("chair")] IReadOnlyList<RerankerAssessmentDto>? Chair,
    [property: JsonPropertyName("monitor")] IReadOnlyList<RerankerAssessmentDto>? Monitor,
    [property: JsonPropertyName("lamp")] IReadOnlyList<RerankerAssessmentDto>? Lamp,
    [property: JsonPropertyName("plant")] IReadOnlyList<RerankerAssessmentDto>? Plant,
    [property: JsonPropertyName("bean_bag")] IReadOnlyList<RerankerAssessmentDto>? BeanBag,
    [property: JsonPropertyName("coffee_machine")] IReadOnlyList<RerankerAssessmentDto>? CoffeeMachine)
{
    public IEnumerable<(string Component, IReadOnlyList<RerankerAssessmentDto> Assessments)> EveryComponent()
    {
        yield return ("desk", Desk ?? []);
        yield return ("chair", Chair ?? []);
        yield return ("monitor", Monitor ?? []);
        yield return ("lamp", Lamp ?? []);
        yield return ("plant", Plant ?? []);
        yield return ("bean_bag", BeanBag ?? []);
        yield return ("coffee_machine", CoffeeMachine ?? []);
    }
}
