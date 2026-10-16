using System.Globalization;
using System.Text;

namespace CoreRentalNet.CatalogIngestion.Seeding;

/// <summary>Renders the SeedProductEmbeddings migration from a seed lifted out of the vector file.</summary>
/// <remarks>
/// <para>
/// <b>The recipe is an <c>InsertData</c>; the vectors are <c>Sql</c>.</b> The recipe is an ordinary mapped
/// table, so EF can seed it. The vector table is a <c>vec0</c> virtual table that is not in the model, so its
/// rows are written as explicit SQL — the same split the migration that creates them uses.
/// </para>
/// <para>
/// <b>The bytes are carried, not re-encoded.</b> Each vector is written as the hex of the blob the file
/// holds, so the seed reproduces the stored bytes exactly rather than round-tripping them through floats.
/// </para>
/// <para>
/// <b>The statements are tabulated as C# literals.</b> Product names and descriptions carry quotes and
/// newlines, and a raw string literal would be broken by either; escaping into an ordinary literal keeps the
/// data intact and the generated file compiling.
/// </para>
/// </remarks>
internal static class SeedMigrationWriter
{
    /// <summary>How many rows one <c>Sql</c> statement carries, so no single literal is disproportionate.</summary>
    private const int VectorsPerStatement = 100;

    private const string VectorTable = "Discovery_ProductVector";
    private const string RecipeTable = "Discovery_ProductVectorRecipe";

    /// <summary>Overwrites the shell with the recipe and every stored passage.</summary>
    public static void Write(string migrationPath, EmbeddingSeed seed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationPath);
        ArgumentNullException.ThrowIfNull(seed);

        File.WriteAllText(migrationPath, Render(seed));
    }

    private static string Render(EmbeddingSeed seed)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;");
        builder.AppendLine();
        builder.AppendLine("#nullable disable");
        builder.AppendLine();
        builder.AppendLine("namespace CoreRentalNet.Modules.Discovery.Infrastructure.Migrations");
        builder.AppendLine("{");
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public partial class SeedProductEmbeddings : Migration");
        builder.AppendLine("    {");

        Up(builder, seed);
        Down(builder);

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static void Up(StringBuilder builder, EmbeddingSeed seed)
    {
        builder.AppendLine("        /// <inheritdoc />");
        builder.AppendLine("        protected override void Up(MigrationBuilder migrationBuilder)");
        builder.AppendLine("        {");

        Recipe(builder, seed.Recipe);

        for (var start = 0; start < seed.Vectors.Count; start += VectorsPerStatement)
        {
            Statement(builder, seed.Vectors.Skip(start).Take(VectorsPerStatement).ToArray());
        }

        builder.AppendLine("        }");
        builder.AppendLine();
    }

    private static void Recipe(StringBuilder builder, SeedRecipe recipe)
    {
        builder.AppendLine("            migrationBuilder.InsertData(");
        builder.AppendLine($"                table: \"{RecipeTable}\",");
        builder.AppendLine("                columns: new[]");
        builder.AppendLine("                {");
        builder.AppendLine("                    \"modelId\", \"width\", \"composition\", \"catalogueHash\", \"created_at\",");
        builder.AppendLine("                },");
        builder.AppendLine(
            "                values: new object[] { "
            + $"{Literal(recipe.ModelId)}, {recipe.Width.ToString(CultureInfo.InvariantCulture)}, "
            + $"{Literal(recipe.Composition)}, {Literal(recipe.CatalogueHash)}, {Literal(recipe.CreatedAt)} }});");
        builder.AppendLine();
    }

    private static void Statement(StringBuilder builder, IReadOnlyList<SeedVector> batch)
    {
        var sql = new StringBuilder($"INSERT INTO {VectorTable} (skuNo, embedding, id, name, description, created_at) VALUES");

        for (var index = 0; index < batch.Count; index++)
        {
            sql.Append(index == 0 ? "\n" : ",\n");
            sql.Append(Row(batch[index]));
        }

        sql.Append(';');

        builder.AppendLine("            migrationBuilder.Sql(");
        builder.AppendLine($"                {Literal(sql.ToString())});");
        builder.AppendLine();
    }

    private static void Down(StringBuilder builder)
    {
        builder.AppendLine("        /// <inheritdoc />");
        builder.AppendLine("        protected override void Down(MigrationBuilder migrationBuilder)");
        builder.AppendLine("        {");
        builder.AppendLine($"            migrationBuilder.Sql(\"DELETE FROM {VectorTable};\");");
        builder.AppendLine();
        builder.AppendLine($"            migrationBuilder.Sql(\"DELETE FROM {RecipeTable};\");");
        builder.AppendLine("        }");
    }

    private static string Row(SeedVector vector)
        => $"({Sql(vector.SkuNo)}, X'{vector.EmbeddingHex}', {Sql(vector.Id)}, "
            + $"{Sql(vector.Name)}, {Sql(vector.Description)}, {Sql(vector.CreatedAt)})";

    /// <summary>The value as a SQLite string literal, with its own quotes doubled.</summary>
    private static string Sql(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    /// <summary>The text as a C# literal, so quotes, backslashes and newlines survive into the generated source.</summary>
    private static string Literal(string value)
        => "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            + "\"";
}
