using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class ScopeHouseholdCategoryNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubGroups_GroupId_Name",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubGroups_Key",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Key",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_Household_Group_Name",
                table: "SubGroups",
                columns: new[] { "HouseholdId", "GroupId", "Name" },
                unique: true,
                filter: "NOT \"IsSystem\"");

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_Household_Key",
                table: "SubGroups",
                columns: new[] { "HouseholdId", "Key" },
                unique: true,
                filter: "NOT \"IsSystem\"");

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_System_Group_Name",
                table: "SubGroups",
                columns: new[] { "GroupId", "Name" },
                unique: true,
                filter: "\"IsSystem\"");

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_System_Key",
                table: "SubGroups",
                column: "Key",
                unique: true,
                filter: "\"IsSystem\"");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Household_Key",
                table: "Categories",
                columns: new[] { "HouseholdId", "Key" },
                unique: true,
                filter: "NOT \"IsSystem\"");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Household_Name",
                table: "Categories",
                columns: new[] { "HouseholdId", "Name" },
                unique: true,
                filter: "NOT \"IsSystem\"");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_System_Key",
                table: "Categories",
                column: "Key",
                unique: true,
                filter: "\"IsSystem\"");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_System_Name",
                table: "Categories",
                column: "Name",
                unique: true,
                filter: "\"IsSystem\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubGroups_Household_Group_Name",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubGroups_Household_Key",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubGroups_System_Group_Name",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubGroups_System_Key",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Household_Key",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Household_Name",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_System_Key",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_System_Name",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_GroupId_Name",
                table: "SubGroups",
                columns: new[] { "GroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_Key",
                table: "SubGroups",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Key",
                table: "Categories",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }
    }
}
