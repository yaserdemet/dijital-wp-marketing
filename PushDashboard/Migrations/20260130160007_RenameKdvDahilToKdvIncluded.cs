using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class RenameKdvDahilToKdvIncluded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceIncludesVat",
                table: "ProductSliderItems",
                newName: "KdvIncluded");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KdvIncluded",
                table: "ProductSliderItems",
                newName: "PriceIncludesVat");
        }
    }
}
