using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstLastNameToStockReminderSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "StockReminderWidgetSubmissions");
        }
    }
}
