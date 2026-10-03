using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class ScopeHouseholdData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "SubGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "PlaidItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_HouseholdId",
                table: "SubGroups",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaidItems_HouseholdId",
                table: "PlaidItems",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_HouseholdId",
                table: "Categories",
                column: "HouseholdId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Households_HouseholdId",
                table: "Categories",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaidItems_Households_HouseholdId",
                table: "PlaidItems",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubGroups_Households_HouseholdId",
                table: "SubGroups",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Households_HouseholdId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaidItems_Households_HouseholdId",
                table: "PlaidItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SubGroups_Households_HouseholdId",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubGroups_HouseholdId",
                table: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_PlaidItems_HouseholdId",
                table: "PlaidItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_HouseholdId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "SubGroups");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "PlaidItems");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Categories");
        }
    }
}
