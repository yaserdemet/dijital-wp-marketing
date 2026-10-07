using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class MoveVoucherSettingsToPrize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoucherCodeFormat",
                table: "GiftWheelSettings");

            migrationBuilder.DropColumn(
                name: "VoucherMode",
                table: "GiftWheelSettings");

            migrationBuilder.DropColumn(
                name: "VoucherPrefix",
                table: "GiftWheelSettings");

            migrationBuilder.DropColumn(
                name: "VoucherValidityDays",
                table: "GiftWheelSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherCodeFormat",
                table: "GiftWheelSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoucherMode",
                table: "GiftWheelSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoucherPrefix",
                table: "GiftWheelSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoucherValidityDays",
                table: "GiftWheelSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
