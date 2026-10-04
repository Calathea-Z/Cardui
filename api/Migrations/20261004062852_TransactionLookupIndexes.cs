using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class TransactionLookupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ArchivedAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Date",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_Date",
                table: "Transactions",
                columns: new[] { "AccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_Uncategorized",
                table: "Transactions",
                column: "AccountId",
                filter: "\"CategoryId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_Uncategorized",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ArchivedAt",
                table: "Transactions",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date",
                table: "Transactions",
                column: "Date");
        }
    }
}
