using AwesomeAssertions;
using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Metrics;

/// <summary>The rule table is applied to the recommendation itself, without a model call.</summary>
public sealed class ConstraintRuleBreakdownTests
{
    [Fact]
    public void A_workspace_inside_every_limit_is_compliant()
    {
        var report = ConstraintRuleBreakdown.Evaluate(
        [
            Line("Desk", "DSKB08XN4JDR", 1),
            Line("Chair", "CHAB09RLYPZ1", 1),
            Line("Monitor", "MONB086Q776F", 2),
        ]);

        report.Compliant.Should().BeTrue();
        report.Violations.Should().BeEmpty();
    }

    [Fact]
    public void A_two_pack_line_contributes_its_quantity_not_its_line_count()
    {
        var report = ConstraintRuleBreakdown.Evaluate(
        [
            Line("Desk", "DSKB08XN4JDR", 1),
            Line("Chair", "CHAB09RLYPZ1", 1),
            Line("Monitor", "MONB086Q776F", 2),
            Line("Monitor", "MONB09GS97F5", 2),
        ]);

        report.Compliant.Should().BeFalse();
        report.Violations.Should().Equal("monitor");
        report.Rules.Single(rule => rule.Rule == "monitor").Actual.Should().Be(4);
    }

    [Fact]
    public void A_missing_chair_is_named_as_the_broken_rule()
    {
        var report = ConstraintRuleBreakdown.Evaluate([Line("Desk", "DSKB08XN4JDR", 1)]);

        report.Compliant.Should().BeFalse();
        report.Violations.Should().Equal("chair");
    }

    [Fact]
    public void The_slot_names_the_agent_uses_map_onto_the_rule_names()
    {
        var report = ConstraintRuleBreakdown.Evaluate(
        [
            Line("Desk", "DSKB08XN4JDR", 1),
            Line("Chair", "CHAB09RLYPZ1", 1),
            Line("RelaxZone", "BBGB0BK4J6K8", 2),
            Line("CoffeeStation", "CFEB08SCMNR9", 1),
        ]);

        report.Violations.Should().Equal("bean bag");
        report.Rules.Single(rule => rule.Rule == "coffee machine").Compliant.Should().BeTrue();
    }

    private static RagSuggestedLine Line(string slot, string sku, int quantity) => new(slot, sku, quantity);
}
