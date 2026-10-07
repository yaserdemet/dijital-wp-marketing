using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class GiftWheelSpinIsSuccessfulAndRemoveFallbackVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FallbackVoucherCode",
                table: "GiftWheelSettings");

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessful",
                table: "GiftWheelSpins",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuccessful",
                table: "GiftWheelSpins");

            migrationBuilder.AddColumn<string>(
                name: "FallbackVoucherCode",
                table: "GiftWheelSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
