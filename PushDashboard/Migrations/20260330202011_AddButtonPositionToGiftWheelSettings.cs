using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddButtonPositionToGiftWheelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ButtonBottomPx",
                table: "GiftWheelSettings",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<int>(
                name: "ButtonMobileBottomPx",
                table: "GiftWheelSettings",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<string>(
                name: "ButtonPosition",
                table: "GiftWheelSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "right");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ButtonBottomPx",
                table: "GiftWheelSettings");

            migrationBuilder.DropColumn(
                name: "ButtonMobileBottomPx",
                table: "GiftWheelSettings");

            migrationBuilder.DropColumn(
                name: "ButtonPosition",
                table: "GiftWheelSettings");
        }
    }
}
