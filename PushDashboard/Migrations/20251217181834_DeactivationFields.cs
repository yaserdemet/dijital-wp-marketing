using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class DeactivationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "CompanyIntegrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeactivatedByService",
                table: "CompanyIntegrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "CompanyIntegrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "CompanyIntegrations");

            migrationBuilder.DropColumn(
                name: "DeactivatedByService",
                table: "CompanyIntegrations");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "CompanyIntegrations");
        }
    }
}
