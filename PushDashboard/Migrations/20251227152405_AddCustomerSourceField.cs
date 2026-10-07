using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSourceField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailPermissionGranted",
                table: "GiftWheelSpins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsPermissionGranted",
                table: "GiftWheelSpins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailPermissionGranted",
                table: "GiftWheelSpins");

            migrationBuilder.DropColumn(
                name: "SmsPermissionGranted",
                table: "GiftWheelSpins");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Customers");
        }
    }
}
