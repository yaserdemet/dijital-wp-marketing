using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDiscountAmountToGiftWheelSpin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderNetAmount",
                table: "GiftWheelSpins",
                newName: "OrderDiscountAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderDiscountAmount",
                table: "GiftWheelSpins",
                newName: "OrderNetAmount");
        }
    }
}
