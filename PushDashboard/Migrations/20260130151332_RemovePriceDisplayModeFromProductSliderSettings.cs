using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriceDisplayModeFromProductSliderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceDisplayMode",
                table: "ProductSliderSettings");

            migrationBuilder.AddColumn<bool>(
                name: "PriceIncludesVat",
                table: "ProductSliderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceIncludesVat",
                table: "ProductSliderItems");

            migrationBuilder.AddColumn<string>(
                name: "PriceDisplayMode",
                table: "ProductSliderSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
