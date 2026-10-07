using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileDelayMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MobileDelayMinutes",
                table: "ExitSurveys",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MobileDelayMinutes",
                table: "ExitSurveys");
        }
    }
}
