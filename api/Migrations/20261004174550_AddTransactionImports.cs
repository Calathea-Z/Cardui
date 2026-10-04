using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImportId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransactionImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImportedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UndoneAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionImports_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionImports_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ImportId",
                table: "Transactions",
                column: "ImportId",
                filter: "\"ImportId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionImports_AccountId",
                table: "TransactionImports",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionImports_HouseholdId_CreatedAt",
                table: "TransactionImports",
                columns: new[] { "HouseholdId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_TransactionImports_ImportId",
                table: "Transactions",
                column: "ImportId",
                principalTable: "TransactionImports",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_TransactionImports_ImportId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "TransactionImports");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ImportId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ImportId",
                table: "Transactions");
        }
    }
}
