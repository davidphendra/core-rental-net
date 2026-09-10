using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreRentalNet.Modules.Rentals.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rentals_Invoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    RentalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeriodIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals_Invoice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rentals_NumberSequence",
                columns: table => new
                {
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals_NumberSequence", x => new { x.Kind, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Rentals_Rental",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AccessTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AnchorDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PlacedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DeliveryScheduledFor = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ActivatedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CancellationRequestedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals_Rental", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rentals_InvoiceLine",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitMonthlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals_InvoiceLine", x => new { x.InvoiceId, x.Sku });
                    table.ForeignKey(
                        name: "FK_Rentals_InvoiceLine_Rentals_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Rentals_Invoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rentals_RentalLine",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RentalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitMonthlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rentals_RentalLine", x => new { x.RentalId, x.Sku });
                    table.ForeignKey(
                        name: "FK_Rentals_RentalLine_Rentals_Rental_RentalId",
                        column: x => x.RentalId,
                        principalTable: "Rentals_Rental",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_Invoice_Number",
                table: "Rentals_Invoice",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_Invoice_RentalId_PeriodIndex",
                table: "Rentals_Invoice",
                columns: new[] { "RentalId", "PeriodIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_Rental_AccessTokenHash",
                table: "Rentals_Rental",
                column: "AccessTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_Rental_Number",
                table: "Rentals_Rental",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rentals_InvoiceLine");

            migrationBuilder.DropTable(
                name: "Rentals_NumberSequence");

            migrationBuilder.DropTable(
                name: "Rentals_RentalLine");

            migrationBuilder.DropTable(
                name: "Rentals_Invoice");

            migrationBuilder.DropTable(
                name: "Rentals_Rental");
        }
    }
}
