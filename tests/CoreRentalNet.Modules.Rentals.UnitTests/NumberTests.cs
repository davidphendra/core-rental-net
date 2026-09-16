using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using Xunit;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

public sealed class NumberTests
{
    [Fact] // CO-14
    public void An_order_number_reads_as_a_person_would_say_it()
    {
        RentalNumber.Of(2026, 1).Value.Should().Be("CR-2026-0001");
        RentalNumber.Of(2026, 42).Value.Should().Be("CR-2026-0042");
        RentalNumber.Of(2026, 9999).Value.Should().Be("CR-2026-9999");
    }

    [Fact] // CO-14
    public void An_invoice_number_has_its_own_prefix()
    {
        InvoiceNumber.Of(2026, 7).Value.Should().Be("INV-2026-0007");
    }

    [Fact] // CO-14
    public void Numbers_round_trip_through_text()
    {
        var number = RentalNumber.Of(2026, 123);

        RentalNumber.Parse(number.Value).Should().Be(number);
        RentalNumber.TryParse("cr-2026-0123", out var loose).Should().BeTrue();
        loose.Should().Be(number);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CR-2026")]
    [InlineData("XX-2026-0001")]
    [InlineData("CR-1999-0001")]
    [InlineData("CR-2026-0000")]
    [InlineData("nonsense")]
    public void A_malformed_order_number_is_refused(string value)
    {
        RentalNumber.TryParse(value, out _).Should().BeFalse();

        var action = () => RentalNumber.Parse(value);
        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_sequence_beyond_the_format_is_refused()
    {
        var action = () => RentalNumber.Of(2026, 10_000);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Order_and_invoice_numbers_are_different_types_on_purpose()
    {
        InvoiceNumber.TryParse("CR-2026-0001", out _).Should().BeFalse("an order number is not an invoice number");
        RentalNumber.TryParse("INV-2026-0001", out _).Should().BeFalse();
    }
}
