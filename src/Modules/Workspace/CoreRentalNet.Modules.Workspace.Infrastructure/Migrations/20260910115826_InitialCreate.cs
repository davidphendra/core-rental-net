using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workspace_Draft",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DraftTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspace_Draft", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspace_SlotAssignment",
                columns: table => new
                {
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    DraftId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspace_SlotAssignment", x => new { x.DraftId, x.Slot });
                    table.ForeignKey(
                        name: "FK_Workspace_SlotAssignment_Workspace_Draft_DraftId",
                        column: x => x.DraftId,
                        principalTable: "Workspace_Draft",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workspace_Draft_DraftTokenHash",
                table: "Workspace_Draft",
                column: "DraftTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Workspace_SlotAssignment");

            migrationBuilder.DropTable(
                name: "Workspace_Draft");
        }
    }
}
