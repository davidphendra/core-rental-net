using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Catalog_Product",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    SubCategory = table.Column<int>(type: "INTEGER", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Image = table.Column<string>(type: "TEXT", nullable: false),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    BestFor = table.Column<string>(type: "TEXT", nullable: false),
                    NotFor = table.Column<string>(type: "TEXT", nullable: false),
                    Attributes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_Product", x => x.Sku);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Product_Category_SubCategory",
                table: "Catalog_Product",
                columns: new[] { "Category", "SubCategory" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Catalog_Product");
        }
    }
}
