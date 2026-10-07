using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateStockReminderToWidgetSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockReminderWidgetSubmissions_StockReminderWidgetSettings_~",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.AlterColumn<int>(
                name: "StockReminderWidgetSettingsId",
                table: "StockReminderWidgetSubmissions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalAlarmId",
                table: "StockReminderWidgetSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExternalMemberId",
                table: "StockReminderWidgetSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNotified",
                table: "StockReminderWidgetSubmissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "StockReminderWidgetSubmissions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotifiedAt",
                table: "StockReminderWidgetSubmissions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "StockReminderWidgetSubmissions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceWithTax",
                table: "StockReminderWidgetSubmissions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductUrl",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "widget");

            migrationBuilder.AddColumn<string>(
                name: "StockCode",
                table: "StockReminderWidgetSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalStock",
                table: "StockReminderWidgetSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSubmissions_CompanyId_ExternalAlarmId",
                table: "StockReminderWidgetSubmissions",
                columns: new[] { "CompanyId", "ExternalAlarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSubmissions_CompanyId_IsNotified",
                table: "StockReminderWidgetSubmissions",
                columns: new[] { "CompanyId", "IsNotified" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSubmissions_CompanyId_ProductId",
                table: "StockReminderWidgetSubmissions",
                columns: new[] { "CompanyId", "ProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockReminderWidgetSubmissions_StockReminderWidgetSettings_~",
                table: "StockReminderWidgetSubmissions",
                column: "StockReminderWidgetSettingsId",
                principalTable: "StockReminderWidgetSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockReminderWidgetSubmissions_StockReminderWidgetSettings_~",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_StockReminderWidgetSubmissions_CompanyId_ExternalAlarmId",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_StockReminderWidgetSubmissions_CompanyId_IsNotified",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_StockReminderWidgetSubmissions_CompanyId_ProductId",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "ExternalAlarmId",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "ExternalMemberId",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "IsNotified",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "NotifiedAt",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "PriceWithTax",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "ProductUrl",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "StockCode",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.DropColumn(
                name: "TotalStock",
                table: "StockReminderWidgetSubmissions");

            migrationBuilder.AlterColumn<int>(
                name: "StockReminderWidgetSettingsId",
                table: "StockReminderWidgetSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockReminderWidgetSubmissions_StockReminderWidgetSettings_~",
                table: "StockReminderWidgetSubmissions",
                column: "StockReminderWidgetSettingsId",
                principalTable: "StockReminderWidgetSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
