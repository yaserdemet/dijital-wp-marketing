using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddCombineWithOtherCampaignsToGiftWheelPrize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CombineWithOtherCampaigns",
                table: "GiftWheelPrizes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GiftVoucherUsed",
                table: "FirstOrderCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "GiftVoucherUsedAt",
                table: "FirstOrderCustomers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CombineWithOtherCampaigns",
                table: "GiftWheelPrizes");

            migrationBuilder.DropColumn(
                name: "GiftVoucherUsed",
                table: "FirstOrderCustomers");

            migrationBuilder.DropColumn(
                name: "GiftVoucherUsedAt",
                table: "FirstOrderCustomers");
        }
    }
}
