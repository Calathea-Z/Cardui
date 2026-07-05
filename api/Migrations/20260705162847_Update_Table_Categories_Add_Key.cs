using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cardui.Api.Migrations
{
    /// <inheritdoc />
    public partial class Update_Table_Categories_Add_Key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                                     UPDATE "Categories"
                                     SET "Key" = CASE "Name"
                                         WHEN 'Income' THEN 'income'
                                         WHEN 'Groceries' THEN 'groceries'
                                         WHEN 'Dining' THEN 'dining'
                                         WHEN 'Bills' THEN 'bills'
                                         WHEN 'Transport' THEN 'transport'
                                         WHEN 'Shopping' THEN 'shopping'
                                         WHEN 'Entertainment' THEN 'entertainment'
                                         WHEN 'Transfers' THEN 'transfers'
                                         WHEN 'Uncategorized' THEN 'uncategorized'
                                         ELSE lower(regexp_replace(trim("Name"), '\s+', '-', 'g'))
                                     END;
                                 """);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Key",
                table: "Categories",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Key",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Categories");
        }
    }
}
