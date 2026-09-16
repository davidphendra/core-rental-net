using System.ComponentModel;
using CoreRentalNet.BuildingBlocks.Application;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

/// <summary>A vocabulary for the attribute readers: one member carrying both facts, one bare.</summary>
internal enum SampleVocabulary
{
    [Description("The annotated kind")]
    [Glyph("known")]
    Annotated,

    Unannotated,
}
