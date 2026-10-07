using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintFromStockReminderExternalAlarm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReminderItem_Company_ExternalAlarm",
                table: "StockReminderItems");

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderItem_Company_ExternalAlarm",
                table: "StockReminderItems",
                columns: new[] { "CompanyId", "ExternalAlarmId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReminderItem_Company_ExternalAlarm",
                table: "StockReminderItems");

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderItem_Company_ExternalAlarm",
                table: "StockReminderItems",
                columns: new[] { "CompanyId", "ExternalAlarmId" },
                unique: true);
        }
    }
}
