using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFieldsToGiftWheelSpin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OrderAmount",
                table: "GiftWheelSpins",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderDate",
                table: "GiftWheelSpins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                table: "GiftWheelSpins",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderAmount",
                table: "GiftWheelSpins");

            migrationBuilder.DropColumn(
                name: "OrderDate",
                table: "GiftWheelSpins");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "GiftWheelSpins");
        }
    }
}
