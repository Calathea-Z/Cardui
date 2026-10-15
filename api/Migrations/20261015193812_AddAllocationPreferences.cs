using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllocationPreferences",
                columns: table => new
                {
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariableIncomeDebtPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    VariableIncomeSavingsPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    WindfallDebtPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    WindfallSavingsPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationPreferences", x => x.HouseholdId);
                    table.ForeignKey(
                        name: "FK_AllocationPreferences_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllocationPreferences");
        }
    }
}
