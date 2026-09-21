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
                name: "Discovery_CatalogVector",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Embedding = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discovery_CatalogVector", x => x.Sku);
                });

            migrationBuilder.CreateTable(
                name: "Discovery_Index",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Composition = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CatalogueHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discovery_Index", x => x.Name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Discovery_CatalogVector");

            migrationBuilder.DropTable(
                name: "Discovery_Index");
        }
    }
}
