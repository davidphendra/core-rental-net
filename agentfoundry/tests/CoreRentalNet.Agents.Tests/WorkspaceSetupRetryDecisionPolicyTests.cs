using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The attempt count and search progress decide whether another attempt is worth running.</summary>
public sealed class WorkspaceSetupRetryDecisionPolicyTests
{
    private readonly WorkspaceSetupRetryDecisionPolicy _policy = new(maximumAttemptCount: 3);

    [Fact]
    public void An_acceptable_review_ends_the_run()
    {
        var decision = _policy.DecideRetryAfterReview(
            completedAttemptCount: 1,
            new WorkspaceSetupReviewResult(IsAcceptable: true, [], null));

        decision.Should().Be(WorkspaceSetupRetryDecision.Accepted);
    }

    [Fact]
    public void A_rejected_review_retries_when_the_search_changes()
    {
        var decision = _policy.DecideRetryAfterReview(
            completedAttemptCount: 1,
            Rejected(),
            retryWouldChangeSearch: true);

        decision.Should().Be(WorkspaceSetupRetryDecision.RetryWithRephrasing);
    }

    [Fact]
    public void A_repeated_search_ends_the_run_instead_of_spending_another_attempt()
    {
        var decision = _policy.DecideRetryAfterReview(
            completedAttemptCount: 1,
            Rejected(),
            retryWouldChangeSearch: false);

        decision.Should().Be(WorkspaceSetupRetryDecision.AttemptsExhausted);
    }

    [Fact]
    public void The_attempt_limit_still_ends_the_run()
    {
        var decision = _policy.DecideRetryAfterReview(
            completedAttemptCount: 3,
            Rejected(),
            retryWouldChangeSearch: true);

        decision.Should().Be(WorkspaceSetupRetryDecision.AttemptsExhausted);
    }

    private static WorkspaceSetupReviewResult Rejected()
        => new(
            IsAcceptable: false,
            Issues: [new WorkspaceSetupReviewIssue("MISSING_SLOT", "the desk is missing")],
            Summary: null);
}
