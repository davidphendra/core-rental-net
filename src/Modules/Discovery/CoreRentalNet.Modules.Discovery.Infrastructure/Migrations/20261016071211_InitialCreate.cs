using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Discovery_ProductVectorRecipe",
                columns: table => new
                {
                    modelId = table.Column<string>(type: "TEXT", nullable: false),
                    width = table.Column<int>(type: "INTEGER", nullable: false),
                    composition = table.Column<string>(type: "TEXT", nullable: false),
                    catalogueHash = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                });

            // Not EF's to author: the vector table is a vec0 virtual table, and only explicit SQL can
            // create it. The columns are the ingestion file's own, so the store this migration builds is
            // the store that file held.
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE Discovery_ProductVector USING vec0(
                    skuNo        TEXT,
                    embedding    float[384] distance_metric=cosine,
                    +id          TEXT,
                    +name        TEXT,
                    +description TEXT,
                    +created_at  TEXT
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS Discovery_ProductVector;");

            migrationBuilder.DropTable(
                name: "Discovery_ProductVectorRecipe");
        }
    }
}
