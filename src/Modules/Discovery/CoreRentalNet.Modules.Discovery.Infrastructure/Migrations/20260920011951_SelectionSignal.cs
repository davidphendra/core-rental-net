using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreRentalNet.Modules.Discovery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SelectionSignal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Discovery_Selection",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DecayedOffered = table.Column<double>(type: "REAL", nullable: false),
                    DecayedChosen = table.Column<double>(type: "REAL", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discovery_Selection", x => x.Sku);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Discovery_Selection");
        }
    }
}
