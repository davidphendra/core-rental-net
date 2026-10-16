using System.Globalization;
using System.Text;
using System.Text.Json;
using CoreRentalNet.CatalogPopulate.Catalog;

namespace CoreRentalNet.CatalogPopulate.Writing;

/// <summary>Renders the SeedProducts migration from the generated catalogue.</summary>
/// <remarks>
/// <para>
/// <b>The migration is the catalogue's seed:</b> one <c>InsertData</c> row per product, and a <c>DeleteData</c>
/// that removes them by SKU. It is written into the shell <c>dotnet ef migrations add SeedProducts</c> created,
/// so EF keeps the file's identity and the model snapshot.
/// </para>
/// <para>
/// <b>The column list mirrors the store's mapping and is not maintained by hand.</b> The <c>SeedProducts</c>
/// architecture test compares it against <c>CatalogProduct</c>'s mapped columns, so a change to the entity that
/// is not carried here fails the build rather than the migration.
/// </para>
/// </remarks>
internal static class SeedMigrationWriter
{
    /// <summary>Overwrites the shell with one row per product, in catalogue order.</summary>
    public static void Write(string migrationPath, IReadOnlyList<ProductRecord> products)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationPath);
        ArgumentNullException.ThrowIfNull(products);

        File.WriteAllText(migrationPath, Render(products));
    }

    private static string Render(IReadOnlyList<ProductRecord> products)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;");
        builder.AppendLine();
        builder.AppendLine("#nullable disable");
        builder.AppendLine();
        builder.AppendLine("namespace CoreRentalNet.Modules.Catalog.Infrastructure.Migrations");
        builder.AppendLine("{");
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public partial class SeedProducts : Migration");
        builder.AppendLine("    {");

        Up(builder, products);
        Down(builder, products);

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static void Up(StringBuilder builder, IReadOnlyList<ProductRecord> products)
    {
        builder.AppendLine("        /// <inheritdoc />");
        builder.AppendLine("        protected override void Up(MigrationBuilder migrationBuilder)");
        builder.AppendLine("        {");
        builder.AppendLine("            migrationBuilder.InsertData(");
        builder.AppendLine("                table: \"Catalog_Product\",");
        builder.AppendLine("                columns: new[]");
        builder.AppendLine("                {");
        builder.AppendLine("                    \"Sku\", \"Name\", \"Category\", \"SubCategory\", \"Amount\", \"Currency\", \"Description\",");
        builder.AppendLine("                    \"Image\", \"IsFeatured\", \"SortOrder\", \"Tags\", \"BestFor\", \"NotFor\", \"Attributes\",");
        builder.AppendLine("                },");
        builder.AppendLine("                values: new object[,]");
        builder.AppendLine("                {");

        for (var index = 0; index < products.Count; index++)
        {
            builder.AppendLine($"                    {Row(products[index], index)},");
        }

        builder.AppendLine("                });");
        builder.AppendLine("        }");
        builder.AppendLine();
    }

    private static void Down(StringBuilder builder, IReadOnlyList<ProductRecord> products)
    {
        builder.AppendLine("        /// <inheritdoc />");
        builder.AppendLine("        protected override void Down(MigrationBuilder migrationBuilder)");
        builder.AppendLine("        {");
        builder.AppendLine("            migrationBuilder.DeleteData(");
        builder.AppendLine("                table: \"Catalog_Product\",");
        builder.AppendLine("                keyColumn: \"Sku\",");
        builder.AppendLine("                keyValues: new object[]");
        builder.AppendLine("                {");

        foreach (var product in products)
        {
            builder.AppendLine($"                    {Literal(product.SkuNo!)},");
        }

        builder.AppendLine("                });");
        builder.AppendLine("        }");
    }

    private static string Row(ProductRecord product, int sortOrder)
        => "{ " + string.Join(", ", Values(product, sortOrder)) + " }";

    private static string[] Values(ProductRecord product, int sortOrder) =>
    [
        Literal(product.SkuNo!),
        Literal(product.Name!),
        CategoryOf(product.Category),
        SubCategoryOf(product.SubCategory),
        product.PricePerMonth.ToString(CultureInfo.InvariantCulture) + "m",
        Literal(product.Currency ?? "IDR"),
        Literal(product.Description!),
        Literal(product.Image ?? string.Empty),
        Featured(product.Badge),
        sortOrder.ToString(CultureInfo.InvariantCulture),
        ArrayJson(product.Metadata?.Tags),
        ArrayJson(product.Metadata?.BestFor),
        ArrayJson(product.Metadata?.NotFor),
        ObjectJson(product.Metadata?.Attributes),
    ];

    private static string CategoryOf(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "chair" => "1",
        "desk" => "2",
        "accessory" => "3",
        _ => throw new InvalidOperationException($"A catalogue row has an unknown category '{value}'."),
    };

    private static string SubCategoryOf(string? value) => string.IsNullOrWhiteSpace(value)
        ? "null"
        : SubCategoryIdOf(value).ToString(CultureInfo.InvariantCulture);

    private static int SubCategoryIdOf(string value) => value.Trim().ToLowerInvariant() switch
    {
        "beanbag" => 1,
        "coffee" => 2,
        "lamp" => 3,
        "monitor" => 4,
        "plant" => 5,
        _ => throw new InvalidOperationException($"A catalogue row has an unknown subcategory '{value}'."),
    };

    private static string Featured(string? badge)
        => string.Equals(badge, "Popular", StringComparison.Ordinal) ? "true" : "false";

    private static string ArrayJson(List<string>? values)
        => Literal(JsonSerializer.Serialize(values ?? new List<string>()));

    private static string ObjectJson(Dictionary<string, string>? values)
        => Literal(JsonSerializer.Serialize(values ?? new Dictionary<string, string>()));

    /// <summary>The value as a C# string literal, so quotes, slashes and newlines survive into the migration.</summary>
    private static string Literal(string value) => "\"" + value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal)
        + "\"";
}
