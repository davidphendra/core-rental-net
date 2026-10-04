using System.Diagnostics;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;

/// <summary>Reads a run's workflow signals off the state it produced, and off the two checks no outcome holds.</summary>
/// <remarks>
/// <para>
/// The state is the one accumulator every node writes to, so the run's outcome is read once, when it is terminal,
/// rather than each completion node knowing which metrics are its own. A search's outcome and a validator's
/// violation are recorded where they are produced, because the state keeps only the attempt in flight.
/// </para>
/// <para>
/// Nothing here is emitted per call: every instrument is bounded by an enum of the domain, so a value that could
/// be a product name or a model's free text never becomes a series.
/// </para>
/// </remarks>
internal static class WorkspaceWorkflowTelemetry
{
    /// <summary>Records what the finished run produced. Called once, by the node that ended it.</summary>
    public static void RecordRunOutcome(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
    {
        var runTags = new TagList { { WorkspaceTelemetry.RunStatus, workspaceSuggestionWorkflowState.RunStatus.ToString() } };

        WorkspaceTelemetry.RunAttemptCount.Record(workspaceSuggestionWorkflowState.CompletedAttemptCount, runTags);

        if (workspaceSuggestionWorkflowState.RunStatus != WorkspaceSuggestionRunStatus.Success)
        {
            WorkspaceTelemetry.RunFailureReason.Add(1, new TagList
            {
                { WorkspaceTelemetry.Reason, FailureReasonOf(workspaceSuggestionWorkflowState) },
            });
        }

        RecordReview(workspaceSuggestionWorkflowState, runTags);
        RecordCandidates(workspaceSuggestionWorkflowState, runTags);
        RecordBudget(workspaceSuggestionWorkflowState);
        RecordComponentCoverage(workspaceSuggestionWorkflowState);
    }

    /// <summary>Records every search of one attempt, by tool and component, as the tool answered it.</summary>
    public static void RecordCatalogue(CatalogueProductRetrievalResult? retrieval)
    {
        if (retrieval is null)
        {
            return;
        }

        foreach (var outcome in retrieval.ComponentSearchOutcomes)
        {
            var searchedNothing = outcome.FoundProductCount == 0;
            var searchTags = new TagList
            {
                { WorkspaceTelemetry.ToolName, outcome.Tool },
                { WorkspaceTelemetry.Outcome, searchedNothing ? "empty" : "found" },
            };
            WorkspaceTelemetry.CatalogueSearch.Add(1, searchTags);

            WorkspaceTelemetry.ProductsRetrieved.Record(outcome.FoundProductCount, new TagList
            {
                { WorkspaceTelemetry.Component, outcome.ComponentCategory.ToString() },
                { WorkspaceTelemetry.ToolName, outcome.Tool },
            });

            if (searchedNothing)
            {
                WorkspaceTelemetry.ZeroResult.Add(1, new TagList
                {
                    { WorkspaceTelemetry.Component, outcome.ComponentCategory.ToString() },
                    { WorkspaceTelemetry.ToolName, outcome.Tool },
                    { WorkspaceTelemetry.Reason, ZeroResultReasonOf(outcome) },
                });
            }
        }
    }

    /// <summary>Records each structural violation by its bounded kind, never by the SKU it names.</summary>
    public static void RecordValidatorViolations(IReadOnlyList<string> violations)
    {
        foreach (var violation in violations)
        {
            WorkspaceTelemetry.ValidatorRejection.Add(1, new TagList
            {
                { WorkspaceTelemetry.Reason, BoundedViolationReasonOf(violation) },
            });
        }
    }

    /// <summary>An empty search the budget caused and one the catalogue caused are different problems.</summary>
    private static string ZeroResultReasonOf(WorkspaceComponentSearchOutcome outcome)
        => string.IsNullOrWhiteSpace(outcome.EmptyResultReason) ? "catalogue" : "budget";

    /// <summary>Why a run ended, from the state alone. Only terminal statuses are asked.</summary>
    private static string FailureReasonOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.RunStatus switch
        {
            WorkspaceSuggestionRunStatus.Rejected => "not_a_workspace_request",
            WorkspaceSuggestionRunStatus.Unavailable when workspaceSuggestionWorkflowState.CatalogueRetrieval is { IsAvailable: false } => "catalogue_unreachable",
            WorkspaceSuggestionRunStatus.Unavailable when workspaceSuggestionWorkflowState.CompletedAttemptCount >= workspaceSuggestionWorkflowState.MaximumAttemptCount => "attempts_exhausted",
            WorkspaceSuggestionRunStatus.Unavailable => "model_error",
            _ => "none",
        };

    /// <summary>The validator's own prefixes, mapped to the four reasons a series may carry.</summary>
    private static string BoundedViolationReasonOf(string violation)
        => violation switch
        {
            "SETUP_HAS_NO_LINES" => "setup_has_no_lines",
            _ when violation.StartsWith("SLOT_NOT_REQUESTED", StringComparison.Ordinal) => "slot_not_requested",
            _ when violation.StartsWith("SKU_NOT_RETRIEVED", StringComparison.Ordinal) => "sku_not_retrieved",
            _ when violation.StartsWith("QUANTITY_ABOVE_CAPACITY", StringComparison.Ordinal) => "quantity_above_capacity",
            _ when violation.StartsWith("MONTHLY_CEILING_EXCEEDED", StringComparison.Ordinal) => "monthly_ceiling_exceeded",
            _ => "other",
        };

    /// <summary>How many grounds a review failed on, and which bounded codes they were.</summary>
    private static void RecordReview(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState, TagList runTags)
    {
        var issues = workspaceSuggestionWorkflowState.WorkspaceSetupReview?.Issues ?? [];
        WorkspaceTelemetry.ReviewIssues.Record(issues.Count, runTags);

        foreach (var issue in issues)
        {
            WorkspaceTelemetry.ReviewIssue.Add(1, new TagList
            {
                { WorkspaceTelemetry.IssueCode, BoundedIssueCodeOf(issue.IssueCode) },
            });
        }
    }

    /// <summary>The seven codes the review prompt enumerates, and `other` for anything else a model writes.</summary>
    private static string BoundedIssueCodeOf(string issueCode)
        => issueCode switch
        {
            "MISSING_SLOT" or "SLOT_PURPOSE_UNSATISFIED" or "MONTHLY_CEILING_EXCEEDED"
                or "SETUPS_NOT_DISTINCT" or "QUERY_MISMATCH" or "COMPONENT_BUDGET_EXCLUDES_ALL"
                or "BUDGET_ALLOCATION_EXCEEDS_TOTAL" => issueCode,
            _ => "other",
        };

    /// <summary>What the run proposed, and what it approved.</summary>
    private static void RecordCandidates(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState, TagList runTags)
    {
        WorkspaceTelemetry.CandidateCount.Record(workspaceSuggestionWorkflowState.ProposedWorkspaceSetups.Count, runTags);

        if (workspaceSuggestionWorkflowState.RunStatus != WorkspaceSuggestionRunStatus.Success)
        {
            return;
        }

        WorkspaceTelemetry.SetupApproved.Add(workspaceSuggestionWorkflowState.ProposedWorkspaceSetups.Count);
        foreach (var approvedSetup in workspaceSuggestionWorkflowState.ProposedWorkspaceSetups)
        {
            WorkspaceTelemetry.SetupLines.Record(approvedSetup.Lines.Count);
        }
    }

    /// <summary>How the ceilings were expressed, and how much room an approved setup left under one.</summary>
    private static void RecordBudget(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
    {
        if (workspaceSuggestionWorkflowState.RequirementExpansion is not { } expansion
            || workspaceSuggestionWorkflowState.RunStatus != WorkspaceSuggestionRunStatus.Success
            || workspaceSuggestionWorkflowState.ProposedWorkspaceSetups.Count == 0)
        {
            return;
        }

        RecordBudgetExpression(expansion);

        var ceiling = workspaceSuggestionWorkflowState.CustomerStatedCeilingMonthly ?? expansion.TotalMonthlyBudget.Amount;
        if (ceiling is not null)
        {
            var cheapestTotal = workspaceSuggestionWorkflowState.ProposedWorkspaceSetups
                .Min(setup => setup.Lines.Sum(line => line.Amount * line.Quantity));
            WorkspaceTelemetry.BudgetHeadroom.Record((double)(ceiling.Value - cheapestTotal));
        }
    }

    /// <summary>Whether each component's budget was the customer's or the rephraser's division.</summary>
    private static void RecordBudgetExpression(WorkspaceRequirementExpansion expansion)
    {
        foreach (var (_, component) in expansion.ComponentExpansions.Every())
        {
            if (component.MonthlyBudget.IsExplicit)
            {
                WorkspaceTelemetry.BudgetExplicit.Add(1);
            }

            if (component.MonthlyBudget.IsDerived)
            {
                WorkspaceTelemetry.BudgetDerived.Add(1);
            }
        }
    }

    /// <summary>Whether the run answered every component it was asked for, and which it left unfilled.</summary>
    private static void RecordComponentCoverage(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
    {
        if (workspaceSuggestionWorkflowState.RequirementExpansion is not { } expansion
            || workspaceSuggestionWorkflowState.RunStatus != WorkspaceSuggestionRunStatus.Success)
        {
            return;
        }

        var requestedSlots = expansion.ComponentExpansions.Every()
            .Where(component => component.Expansion.IsRelevant)
            .Select(component => WorkspaceComponentVocabularyMapping.CompositionSlotFor(component.ComponentCategory))
            .ToHashSet();

        if (requestedSlots.Count == 0)
        {
            return;
        }

        var filledSlots = workspaceSuggestionWorkflowState.ProposedWorkspaceSetups
            .SelectMany(setup => setup.Lines)
            .Select(line => line.Slot)
            .ToHashSet();

        var filledCount = requestedSlots.Count(slot => filledSlots.Contains(slot));
        WorkspaceTelemetry.ComponentFilledRatio.Record((double)filledCount / requestedSlots.Count);

        foreach (var missingSlot in requestedSlots.Where(slot => !filledSlots.Contains(slot)))
        {
            WorkspaceTelemetry.ComponentShortfall.Add(1, new TagList
            {
                { WorkspaceTelemetry.Component, missingSlot.ToString() },
            });
        }
    }
}
