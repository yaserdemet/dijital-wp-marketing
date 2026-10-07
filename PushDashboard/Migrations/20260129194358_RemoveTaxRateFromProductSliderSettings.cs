using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaxRateFromProductSliderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "ProductSliderSettings");

            migrationBuilder.AddColumn<double>(
                name: "TaxRate",
                table: "ProductSliderItems",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "ProductSliderItems");

            migrationBuilder.AddColumn<double>(
                name: "TaxRate",
                table: "ProductSliderSettings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
