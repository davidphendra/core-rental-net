using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Keeps a reviewer's semantic rejection only when both sides of the contradiction are real.</summary>
/// <remarks>
/// <para>
/// A model can reject a product whose name already contains the requested attribute. The reviewer is told to
/// quote the customer's attribute and the product evidence; this policy checks both quotes against the exact
/// composed line the reviewer named, and drops a rejection that is not grounded in it.
/// </para>
/// <para>
/// <b>`SLOT_PURPOSE_UNSATISFIED` and the budget codes are inspected.</b> A semantic rejection needs two
/// grounded quotes on one line. A budget rejection is kept only when the request actually carries a budget.
/// Missing and unrequested slots are facts the workflow can carry without either check.
/// </para>
/// </remarks>
public sealed class WorkspaceSetupReviewEvidencePolicy
{
    private const string SlotPurposeUnsatisfied = "SLOT_PURPOSE_UNSATISFIED";

    private static readonly string[] s_budgetIssueCodes =
    [
        "MONTHLY_CEILING_EXCEEDED",
        "COMPONENT_BUDGET_EXCLUDES_ALL",
        "BUDGET_ALLOCATION_EXCEEDS_TOTAL",
    ];

    /// <summary>The review with every ungrounded semantic rejection removed.</summary>
    public WorkspaceSetupReviewResult Apply(
        WorkspaceSetupReviewResult workspaceSetupReviewResult,
        string customerQuery,
        IReadOnlyList<WorkspaceSetupCandidate> proposedWorkspaceSetups,
        IReadOnlyList<SelectedWorkspaceComponentProduct> selectedWorkspaceCandidates,
        WorkspaceRequirementExpansion? requirementExpansion)
    {
        ArgumentNullException.ThrowIfNull(workspaceSetupReviewResult);
        ArgumentNullException.ThrowIfNull(customerQuery);
        ArgumentNullException.ThrowIfNull(proposedWorkspaceSetups);
        ArgumentNullException.ThrowIfNull(selectedWorkspaceCandidates);

        if (workspaceSetupReviewResult.IsAcceptable)
        {
            return workspaceSetupReviewResult;
        }

        var lines = ReviewerLines(proposedWorkspaceSetups, selectedWorkspaceCandidates);
        var budgetExists = HasBudget(requirementExpansion);
        var groundedIssues = workspaceSetupReviewResult.Issues
            .Where(issue => IsGrounded(issue, customerQuery, lines, budgetExists))
            .ToArray();

        if (groundedIssues.Length == workspaceSetupReviewResult.Issues.Count)
        {
            return workspaceSetupReviewResult;
        }

        return groundedIssues.Length == 0
            ? workspaceSetupReviewResult with { IsAcceptable = true, Issues = [], Summary = null }
            : workspaceSetupReviewResult with { Issues = groundedIssues };
    }

    private static bool IsGrounded(
        WorkspaceSetupReviewIssue issue,
        string customerQuery,
        IReadOnlyList<(WorkspaceSetupLine Line, string Text)> lines,
        bool budgetExists)
    {
        if (s_budgetIssueCodes.Contains(issue.IssueCode, StringComparer.Ordinal))
        {
            return budgetExists;
        }

        if (!string.Equals(issue.IssueCode, SlotPurposeUnsatisfied, StringComparison.Ordinal))
        {
            return true;
        }

        var customerEvidence = issue.CustomerEvidence;
        var productEvidence = issue.ProductEvidence;

        if (string.IsNullOrWhiteSpace(customerEvidence)
            || string.IsNullOrWhiteSpace(productEvidence)
            || !customerQuery.Contains(customerEvidence, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var linesForTheIssue = issue.Slot is { } slot
            ? lines.Where(line => line.Line.Slot == slot).ToArray()
            : lines.Where(line => line.Text.Contains(productEvidence, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (linesForTheIssue.Length == 0
            || !linesForTheIssue.Any(line =>
                line.Text.Contains(productEvidence, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return !linesForTheIssue.Any(line => ContainsEveryWord(line.Text, customerEvidence));
    }

    private static IReadOnlyList<(WorkspaceSetupLine Line, string Text)> ReviewerLines(
        IReadOnlyList<WorkspaceSetupCandidate> proposedWorkspaceSetups,
        IReadOnlyList<SelectedWorkspaceComponentProduct> selectedWorkspaceCandidates)
    {
        var descriptions = selectedWorkspaceCandidates
            .GroupBy(selection => selection.RetrievedProduct.Sku, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().RetrievedProduct.Description,
                StringComparer.OrdinalIgnoreCase);

        return [.. proposedWorkspaceSetups
            .SelectMany(setup => setup.Lines)
            .Select(line => (
                line,
                Text: string.Join(
                    ' ',
                    new[] { line.Name, descriptions.GetValueOrDefault(line.Sku) }
                        .Where(text => !string.IsNullOrWhiteSpace(text)))))];
    }

    private static bool HasBudget(WorkspaceRequirementExpansion? requirementExpansion)
        => requirementExpansion is not null
            && (requirementExpansion.TotalMonthlyBudget.Amount is not null
                || requirementExpansion.ComponentExpansions.Every()
                    .Any(component => component.Expansion.MonthlyBudget.MaximumAmount is not null));

    private static bool ContainsEveryWord(string productText, string customerEvidence)
    {
        var customerWords = customerEvidence
            .Split([' ', '-', '/', ',', '.', ';', ':', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 0)
            .ToArray();

        return customerWords.Length > 0
            && customerWords.All(word => productText.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
