using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryTargetMonths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    CopiedFromYear = table.Column<int>(type: "integer", nullable: true),
                    CopiedFromMonth = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTargetMonths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryTargetMonths_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryTargetMonthId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Rollover = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryTargets_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryTargets_CategoryTargetMonths_CategoryTargetMonthId",
                        column: x => x.CategoryTargetMonthId,
                        principalTable: "CategoryTargetMonths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTargetMonths_HouseholdId_Year_Month",
                table: "CategoryTargetMonths",
                columns: new[] { "HouseholdId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTargets_CategoryId",
                table: "CategoryTargets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTargets_CategoryTargetMonthId_CategoryId",
                table: "CategoryTargets",
                columns: new[] { "CategoryTargetMonthId", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryTargets");

            migrationBuilder.DropTable(
                name: "CategoryTargetMonths");
        }
    }
}
