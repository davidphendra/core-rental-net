namespace CoreRentalNet.CatalogPopulate;

/// <summary>What this invocation of the tool does.</summary>
internal enum Mode
{
    /// <summary>Scan the source files and write the compact input.</summary>
    Extract,

    /// <summary>Read the compact input and write the catalogue.</summary>
    Populate,
}
