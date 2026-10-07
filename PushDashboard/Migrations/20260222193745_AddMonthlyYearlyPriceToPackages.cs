using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyYearlyPriceToPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ModulePackages",
                newName: "YearlyPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "ModulePackages",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "ModulePackages");

            migrationBuilder.RenameColumn(
                name: "YearlyPrice",
                table: "ModulePackages",
                newName: "Price");
        }
    }
}
