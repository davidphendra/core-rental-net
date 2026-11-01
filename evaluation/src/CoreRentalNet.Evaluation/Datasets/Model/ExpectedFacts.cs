using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>Where a case's facts came from, so a report never claims human labelling it did not receive.</summary>
public enum ExpectedFactsSource
{
    Annotated,
    Derived,
}

/// <summary>The facts a case states, together with their provenance.</summary>
public sealed record ExpectedFacts(IReadOnlyList<string> Facts, ExpectedFactsSource Source);
