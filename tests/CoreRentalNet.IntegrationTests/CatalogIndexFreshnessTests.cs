using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure.Indexing;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Whether the vectors the ingestion tool wrote may be searched by this deployment.
/// </summary>
/// <remarks>
/// Each of the four recorded values is exercised separately, because each of them fails the same silent way: a
/// search over vectors built from another catalogue, another model, another width or another text still returns
/// fourteen products and simply ranks them wrongly. Nothing downstream notices, which is why this check exists
/// and why every one of the four has its own test rather than one test for "stale".
/// </remarks>
public sealed class CatalogIndexFreshnessTests
{
    private const int Width = 384;

    private const string Hash = "0f1e2d3c4b5a6978";

    private const string Model = "all-MiniLM-L6-v2-embedding";

    private static CatalogIndexVerdict Check(string path, string hash = Hash, string model = Model, int width = Width)
        => new CatalogIndexFreshness(path).Check(hash, model, width, ProductVectorContract.Composition);

    /// <summary>A file with a recipe in it, as the tool would leave it.</summary>
    private static IngestedVectorFile Built(int width = Width, string model = Model, string? composition = null, string hash = Hash)
        => new IngestedVectorFile()
            .WithVector("DSKB08XN4JDR", new float[width])
            .WithRecipe(model, width, composition ?? ProductVectorContract.Composition, hash);

    [Fact] // SCR-02
    public void Vectors_that_were_never_built_are_not_usable_and_name_the_tool()
    {
        // A file the tool has created but not yet filled: the tables are there and no recipe is.
        using var file = new IngestedVectorFile();

        var verdict = Check(file.Path);

        verdict.IsCurrent.Should().BeFalse();
        // "Never built" and "stale" hide the same feature and want different fixes, so the line says which.
        verdict.Reason.Should().Contain("CatalogIngestion");
    }

    [Fact] // SCR-02
    public void A_file_that_is_not_there_is_not_built_rather_than_an_error()
    {
        // Reading open a SQLite file creates it, so a reader that opened one unconditionally would leave an
        // empty database behind and report the vectors missing for a second reason.
        var absent = Path.Combine(Path.GetTempPath(), $"absent-{Guid.NewGuid():N}", "product_embedding.db");

        var verdict = Check(absent);

        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain("CatalogIngestion");
        File.Exists(absent).Should().BeFalse("a check must not create the file it is checking for");
    }

    [Fact] // SCR-02
    public void Vectors_built_from_this_catalogue_by_this_model_are_usable()
    {
        using var file = Built();

        var verdict = Check(file.Path);

        verdict.IsCurrent.Should().BeTrue();
        verdict.Reason.Should().BeEmpty();
    }

    [Fact] // SCR-02
    public void A_catalogue_that_has_changed_since_the_vectors_were_built_is_stale()
    {
        using var file = Built();

        var verdict = Check(file.Path, hash: "ffffffffffffffff");

        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain("catalogue has changed").And.Contain(Hash).And.Contain("ffffffffffffffff");
    }

    [Fact] // SCR-02
    public void A_model_that_is_not_the_one_recorded_is_stale()
    {
        using var file = Built();

        var verdict = Check(file.Path, model: "text-embedding-3-large");

        // Vectors from two models are not comparable, so this is the case where a search would silently mean
        // nothing rather than fail.
        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain(Model).And.Contain("text-embedding-3-large");
    }

    [Fact] // SCR-02
    public void A_width_that_is_not_the_one_recorded_is_stale()
    {
        using var file = Built();

        var verdict = Check(file.Path, width: 512);

        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain("384").And.Contain("512");
    }

    [Fact] // SCR-02
    public void A_composition_that_the_renderer_no_longer_produces_is_stale()
    {
        // Written into the file rather than produced by the tool, because the tool always records the
        // composition its renderer produces - which is exactly why this case cannot come from the tool and can
        // come from a developer changing the fields a product's text is made of.
        using var file = Built(composition: "name+description/0");

        var verdict = Check(file.Path);

        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain("name+description/0").And.Contain(ProductVectorContract.Composition);
    }
}
