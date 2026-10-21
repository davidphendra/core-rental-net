namespace CoreRentalNet.Evaluation.Cases;

/// <summary>Reads the scope name a caller typed.</summary>
/// <remarks>
/// Names are matched case-insensitively and numbers are not accepted as names: a mistyped scope must fail
/// before a model is called, rather than quietly produce a cheaper run that looks successful.
/// </remarks>
public static class EvaluationScopeParser
{
    /// <summary>The scope a value names, or <see cref="EvaluationScope.Tiny"/> when it is absent.</summary>
    public static EvaluationScope Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EvaluationScope.Tiny;
        }

        var name = Enum.GetNames<EvaluationScope>()
            .FirstOrDefault(candidate => string.Equals(
                candidate,
                value.Trim(),
                StringComparison.OrdinalIgnoreCase));

        return name is null
            ? throw new ArgumentException(
                $"Unknown scope '{value}'. Use tiny, small, half or all.")
            : Enum.Parse<EvaluationScope>(name);
    }
}
