using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SlotHoldsSeveralProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Workspace_SlotAssignment",
                table: "Workspace_SlotAssignment");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workspace_SlotAssignment",
                table: "Workspace_SlotAssignment",
                columns: new[] { "DraftId", "Slot", "Sku" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Workspace_SlotAssignment",
                table: "Workspace_SlotAssignment");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workspace_SlotAssignment",
                table: "Workspace_SlotAssignment",
                columns: new[] { "DraftId", "Slot" });
        }
    }
}
