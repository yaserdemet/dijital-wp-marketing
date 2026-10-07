using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherSettingsToPrize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDiscountedProducts",
                table: "GiftWheelPrizes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoucherCodeFormat",
                table: "GiftWheelPrizes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoucherMode",
                table: "GiftWheelPrizes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoucherPrefix",
                table: "GiftWheelPrizes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDiscountedProducts",
                table: "GiftWheelPrizes");

            migrationBuilder.DropColumn(
                name: "VoucherCodeFormat",
                table: "GiftWheelPrizes");

            migrationBuilder.DropColumn(
                name: "VoucherMode",
                table: "GiftWheelPrizes");

            migrationBuilder.DropColumn(
                name: "VoucherPrefix",
                table: "GiftWheelPrizes");
        }
    }
}
