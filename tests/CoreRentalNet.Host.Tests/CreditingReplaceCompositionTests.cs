using AwesomeAssertions;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Discovery.Application.Selection;
using CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;
using CoreRentalNet.Modules.Workspace.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Crediting the products of a composition a customer actually applied.
/// </summary>
/// <remarks>
/// This is the numerator of the whole selection signal. Before it, an offer was recorded and a choice never was,
/// so every score was zero and retrieval would have ranked nothing while looking perfectly healthy.
/// </remarks>
public sealed class CreditingReplaceCompositionTests
{
    private static ReplaceCompositionCommand Command(params string[] skus)
        => new(
            "raw-token",
            1,
            [.. skus.Select(sku => new ReplaceCompositionLine(SlotId.Desk, sku, 1))]);

    private static CreditingReplaceComposition Decorating(FakeApply apply, RecordingCredit credit)
        => new(apply, credit, NullLoggerFactory.Instance);

    [Fact] // the gap this closes
    public async Task An_applied_composition_credits_exactly_the_products_in_it()
    {
        var credit = new RecordingCredit();

        await Decorating(new FakeApply(), credit)
            .HandleAsync(Command("DSKB08XN4JDR", "CHA449AGLBB0"));

        credit.Credited.Should().ContainSingle();
        credit.Credited[0].Should().Equal("DSKB08XN4JDR", "CHA449AGLBB0");
    }

    [Fact]
    public async Task A_composition_that_names_one_product_twice_still_credits_one_choice()
    {
        var credit = new RecordingCredit();

        await Decorating(new FakeApply(), credit).HandleAsync(Command("MONJVAP81NPQ", "MONJVAP81NPQ"));

        // The decorator hands over what the command said; the service is what makes it one choice, and it is
        // asserted there as well so the rule cannot be lost by either half.
        credit.Credited[0].Should().Equal("MONJVAP81NPQ", "MONJVAP81NPQ");
    }

    [Fact] // a refused apply applied nothing, so there is nothing to credit
    public async Task An_apply_that_was_refused_credits_nothing()
    {
        var credit = new RecordingCredit();
        var refused = new FakeApply { Refuse = true };

        var act = () => Decorating(refused, credit).HandleAsync(Command("DSKB08XN4JDR"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        credit.Credited.Should().BeEmpty("the workspace never changed, so the customer chose nothing");
    }

    [Fact]
    public async Task A_credit_that_cannot_be_recorded_does_not_fail_the_apply()
    {
        // The composition is already written. Losing one credit is a fact to log, not a reason to tell a customer
        // their saved workspace did not save.
        var failing = new RecordingCredit { Fail = true };

        var act = () => Decorating(new FakeApply(), failing).HandleAsync(Command("DSKB08XN4JDR"));

        await act.Should().NotThrowAsync();
    }

    /// <summary>The command handler, recording that it ran and refusing when a test asks it to.</summary>
    private sealed class FakeApply : IReplaceCompositionHandler
    {
        public bool Refuse { get; init; }

        public bool Applied { get; private set; }

        public Task HandleAsync(ReplaceCompositionCommand command, CancellationToken cancellationToken = default)
        {
            if (Refuse)
            {
                throw new InvalidOperationException("the workspace changed underneath the caller");
            }

            Applied = true;

            return Task.CompletedTask;
        }
    }

    /// <summary>The signal, recording what it was asked to credit.</summary>
    private sealed class RecordingCredit : ICreditSelection
    {
        public List<IReadOnlyList<string>> Credited { get; } = [];

        public bool Fail { get; init; }

        public Task<int> CreditAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                throw new InvalidOperationException("the signal could not be written");
            }

            Credited.Add(skus);

            return Task.FromResult(skus.Count);
        }
    }
}
