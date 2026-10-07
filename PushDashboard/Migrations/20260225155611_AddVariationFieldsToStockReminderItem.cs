using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddVariationFieldsToStockReminderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VariationBarcode",
                table: "StockReminderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariationId",
                table: "StockReminderItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariationStockCode",
                table: "StockReminderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariationStockQuantity",
                table: "StockReminderItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VariationBarcode",
                table: "StockReminderItems");

            migrationBuilder.DropColumn(
                name: "VariationId",
                table: "StockReminderItems");

            migrationBuilder.DropColumn(
                name: "VariationStockCode",
                table: "StockReminderItems");

            migrationBuilder.DropColumn(
                name: "VariationStockQuantity",
                table: "StockReminderItems");
        }
    }
}
