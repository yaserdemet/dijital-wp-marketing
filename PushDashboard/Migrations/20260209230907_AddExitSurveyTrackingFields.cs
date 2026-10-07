using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddExitSurveyTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DismissCount",
                table: "ExitSurveys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImpressionCount",
                table: "ExitSurveys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsMobile",
                table: "ExitSurveyResponses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PageUrl",
                table: "ExitSurveyResponses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DismissCount",
                table: "ExitSurveys");

            migrationBuilder.DropColumn(
                name: "ImpressionCount",
                table: "ExitSurveys");

            migrationBuilder.DropColumn(
                name: "IsMobile",
                table: "ExitSurveyResponses");

            migrationBuilder.DropColumn(
                name: "PageUrl",
                table: "ExitSurveyResponses");
        }
    }
}
