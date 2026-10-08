using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEverydaySpendingAndCashToKeep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "TargetDate",
                table: "SavingsGoals",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetAmount",
                table: "SavingsGoals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "FloorAmount",
                table: "SavingsGoals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyAmount",
                table: "SavingsGoals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadyDay",
                table: "SavingsGoals",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_HouseholdId_Floor",
                table: "SavingsGoals",
                column: "HouseholdId",
                unique: true,
                filter: "\"Kind\" = 'Floor'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavingsGoals_HouseholdId_Floor",
                table: "SavingsGoals");

            migrationBuilder.DropColumn(
                name: "FloorAmount",
                table: "SavingsGoals");

            migrationBuilder.DropColumn(
                name: "MonthlyAmount",
                table: "SavingsGoals");

            migrationBuilder.DropColumn(
                name: "ReadyDay",
                table: "SavingsGoals");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "TargetDate",
                table: "SavingsGoals",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetAmount",
                table: "SavingsGoals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
