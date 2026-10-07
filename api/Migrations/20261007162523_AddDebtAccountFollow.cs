using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtAccountFollow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Debts_AccountId",
                table: "Debts");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccountFollowedSince",
                table: "Debts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BalanceOverriddenAt",
                table: "Debts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Debts_AccountId_Followed",
                table: "Debts",
                column: "AccountId",
                unique: true,
                filter: "\"AccountFollowedSince\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Debts_AccountId_Followed",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "AccountFollowedSince",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "BalanceOverriddenAt",
                table: "Debts");

            migrationBuilder.CreateIndex(
                name: "IX_Debts_AccountId",
                table: "Debts",
                column: "AccountId");
        }
    }
}
