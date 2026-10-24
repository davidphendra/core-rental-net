namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Composes the registered bias section builders.</summary>
public sealed class BiasReportBuilder
{
    private readonly IReadOnlyList<IBiasSectionBuilder> _sectionBuilders;

    public BiasReportBuilder(IEnumerable<IBiasSectionBuilder> sectionBuilders)
    {
        ArgumentNullException.ThrowIfNull(sectionBuilders);
        _sectionBuilders = sectionBuilders.ToArray();
    }

    public static BiasReportBuilder CreateDefault()
        => new(
        [
            new FairnessBiasBuilder(),
            new RecommendationBiasBuilder(),
            new VerifierBiasBuilder(),
            new ReasoningBiasBuilder(),
            new ReviewerBiasBuilder(),
            new LanguageBiasBuilder(),
        ]);

    public BiasReport Build(BiasInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var metrics = _sectionBuilders
            .OrderBy(builder => builder.Section)
            .SelectMany(builder => builder.Build(input))
            .ToArray();

        return new BiasReport(metrics);
    }
}
