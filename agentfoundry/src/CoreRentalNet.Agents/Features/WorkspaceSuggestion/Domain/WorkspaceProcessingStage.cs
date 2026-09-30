using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The part of the run a caller is being told about while it goes.</summary>
/// <remarks>
/// Progress, never a business outcome. The two are separate concepts on purpose: the caller is told what is
/// happening in these words while the run's status stays <see cref="WorkspaceSuggestionRunStatus.Processing" />.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<WorkspaceProcessingStage>))]
public enum WorkspaceProcessingStage
{
    [JsonStringEnumMemberName("verifyingRequest")]
    VerifyingRequest,

    [JsonStringEnumMemberName("rephrasingRequirement")]
    RephrasingRequirement,

    [JsonStringEnumMemberName("retrievingCatalogueProducts")]
    RetrievingCatalogueProducts,

    [JsonStringEnumMemberName("rerankingWorkspaceCandidates")]
    RerankingWorkspaceCandidates,

    [JsonStringEnumMemberName("composingWorkspaceSetups")]
    ComposingWorkspaceSetups,

    [JsonStringEnumMemberName("validatingWorkspaceSetups")]
    ValidatingWorkspaceSetups,

    [JsonStringEnumMemberName("reviewingWorkspaceSetups")]
    ReviewingWorkspaceSetups,
}
