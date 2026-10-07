using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "Modules",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "YearlyPrice",
                table: "Modules",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenewal",
                table: "CompanyModules",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRenewalDate",
                table: "CompanyModules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewalCount",
                table: "CompanyModules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionType",
                table: "CompanyModules",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "YearlyPrice",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "AutoRenewal",
                table: "CompanyModules");

            migrationBuilder.DropColumn(
                name: "LastRenewalDate",
                table: "CompanyModules");

            migrationBuilder.DropColumn(
                name: "RenewalCount",
                table: "CompanyModules");

            migrationBuilder.DropColumn(
                name: "SubscriptionType",
                table: "CompanyModules");
        }
    }
}
