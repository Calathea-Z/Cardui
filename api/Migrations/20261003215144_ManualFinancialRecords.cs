using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class ManualFinancialRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Accounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OpeningBalanceDate",
                table: "Accounts",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ArchivedAt",
                table: "Transactions",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ArchivedAt",
                table: "Accounts",
                column: "ArchivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_ArchivedAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_ArchivedAt",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OpeningBalanceDate",
                table: "Accounts");
        }
    }
}
