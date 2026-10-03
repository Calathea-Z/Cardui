using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class IndependentFinancialRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_PlaidItems_PlaidItemId",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "PlaidTransactionId",
                table: "Transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Provenance",
                table: "Transactions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "PlaidSync");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Transactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Plaid");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlaidItemId",
                table: "Accounts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "PlaidAccountId",
                table: "Accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "Accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provenance",
                table: "Accounts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "PlaidSync");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Accounts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Plaid");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_HouseholdId",
                table: "Accounts",
                column: "HouseholdId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Households_HouseholdId",
                table: "Accounts",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_PlaidItems_PlaidItemId",
                table: "Accounts",
                column: "PlaidItemId",
                principalTable: "PlaidItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                UPDATE "Accounts" AS account
                SET "HouseholdId" = plaid_item."HouseholdId"
                FROM "PlaidItems" AS plaid_item
                WHERE account."PlaidItemId" = plaid_item."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Provenance",
                table: "Transactions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "PlaidSync");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "Transactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Plaid");

            migrationBuilder.AlterColumn<string>(
                name: "Provenance",
                table: "Accounts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "PlaidSync");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "Accounts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Plaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Households_HouseholdId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_PlaidItems_PlaidItemId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_HouseholdId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Provenance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Provenance",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "PlaidTransactionId",
                table: "Transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PlaidItemId",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlaidAccountId",
                table: "Accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_PlaidItems_PlaidItemId",
                table: "Accounts",
                column: "PlaidItemId",
                principalTable: "PlaidItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
