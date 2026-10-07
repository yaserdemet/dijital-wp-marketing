using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowResendForSentCustomersToBasketReminderSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowResendForSentCustomers",
                table: "BasketReminderSettings");

            migrationBuilder.AddColumn<bool>(
                name: "AllowResendForSentCustomers",
                table: "BasketReminderSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowResendForSentCustomers",
                table: "BasketReminderSchedules");

            migrationBuilder.AddColumn<bool>(
                name: "AllowResendForSentCustomers",
                table: "BasketReminderSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
