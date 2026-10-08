using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsGoalHouseholdIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_HouseholdId",
                table: "SavingsGoals",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_HouseholdId_Operating",
                table: "SavingsGoals",
                column: "HouseholdId",
                unique: true,
                filter: "\"Kind\" = 'Operating'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavingsGoals_HouseholdId",
                table: "SavingsGoals");

            migrationBuilder.DropIndex(
                name: "IX_SavingsGoals_HouseholdId_Operating",
                table: "SavingsGoals");
        }
    }
}
