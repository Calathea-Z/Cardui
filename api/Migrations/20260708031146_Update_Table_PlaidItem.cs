using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class Update_Table_PlaidItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncCompletedAt",
                table: "PlaidItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "PlaidItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncFailedAt",
                table: "PlaidItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncStartedAt",
                table: "PlaidItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncCompletedAt",
                table: "PlaidItems");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "PlaidItems");

            migrationBuilder.DropColumn(
                name: "LastSyncFailedAt",
                table: "PlaidItems");

            migrationBuilder.DropColumn(
                name: "LastSyncStartedAt",
                table: "PlaidItems");
        }
    }
}
