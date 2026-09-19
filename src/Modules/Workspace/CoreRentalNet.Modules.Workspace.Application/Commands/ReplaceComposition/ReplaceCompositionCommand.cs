namespace CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;

/// <summary>
/// Replace everything the draft holds with a given composition, in one write.
/// </summary>
/// <remarks>
/// <para>
/// Unlike adding or removing one item, this is <b>one act</b>: the composition is replaced whole, so a caller
/// never sees a workspace that is half the old setup and half the new one.
/// </para>
/// <para>
/// <see cref="ExpectedVersion"/> is the version the caller read before deciding what to replace it with. When
/// it no longer matches, the workspace changed in the meantime and the write is refused rather than merged -
/// a customer who has added something in another tab since the suggestion was drawn must not have it silently
/// discarded.
/// </para>
/// <para>
/// The delivery address is not part of a composition and is not in this command at all. That is the point
/// rather than an omission: replacing a setup is not re-addressing it.
/// </para>
/// </remarks>
public sealed record ReplaceCompositionCommand(
    string DraftToken,
    int ExpectedVersion,
    IReadOnlyList<ReplaceCompositionLine> Lines);
