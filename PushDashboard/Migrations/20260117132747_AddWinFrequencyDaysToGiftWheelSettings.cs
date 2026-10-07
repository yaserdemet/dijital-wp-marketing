using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddWinFrequencyDaysToGiftWheelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VoucherUsed",
                table: "GiftWheelSpins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoucherUsedAt",
                table: "GiftWheelSpins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WinFrequencyDays",
                table: "GiftWheelSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoucherUsed",
                table: "GiftWheelSpins");

            migrationBuilder.DropColumn(
                name: "VoucherUsedAt",
                table: "GiftWheelSpins");

            migrationBuilder.DropColumn(
                name: "WinFrequencyDays",
                table: "GiftWheelSettings");
        }
    }
}
