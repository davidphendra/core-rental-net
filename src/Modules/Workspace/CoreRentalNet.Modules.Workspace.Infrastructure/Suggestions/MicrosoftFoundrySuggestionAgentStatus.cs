namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Whether the provider proposed candidates or refused the request, as the contract spells it.</summary>
/// <remarks>
/// Serialized as <c>suggested</c> / <c>notWorkspace</c> / <c>catalogueUnavailable</c> by the camel-case enum
/// converter in <see cref="MicrosoftFoundrySuggestionAgentJson"/>.
/// </remarks>
internal enum MicrosoftFoundrySuggestionAgentStatus
{
    Suggested,
    NotWorkspace,
    CatalogueUnavailable,
}
