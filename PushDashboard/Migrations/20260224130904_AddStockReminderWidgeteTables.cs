using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReminderWidgeteTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppPermission",
                table: "Customers");

            migrationBuilder.CreateTable(
                name: "StockReminderWidgetSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SubmitButtonText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrivacyText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PrivacyUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PhonePlaceholder = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmailPlaceholder = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VariantOptions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VariantLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShowVariantSelector = table.Column<bool>(type: "boolean", nullable: false),
                    ShowPhoneField = table.Column<bool>(type: "boolean", nullable: false),
                    ShowEmailField = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePhone = table.Column<bool>(type: "boolean", nullable: false),
                    ShowConsentCheckbox = table.Column<bool>(type: "boolean", nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TitleColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ButtonColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ButtonTextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VariantBorderColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VariantSelectedColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorderRadius = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnMobile = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnDesktop = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAnimation = table.Column<bool>(type: "boolean", nullable: false),
                    ImpressionCount = table.Column<int>(type: "integer", nullable: false),
                    SubmitCount = table.Column<int>(type: "integer", nullable: false),
                    DismissCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReminderWidgetSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReminderWidgetSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockReminderWidgetSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockReminderWidgetSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockReminderWidgetSettingsId = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SelectedVariant = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConsentGiven = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReminderWidgetSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReminderWidgetSubmissions_StockReminderWidgetSettings_~",
                        column: x => x.StockReminderWidgetSettingsId,
                        principalTable: "StockReminderWidgetSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSettings_CompanyId",
                table: "StockReminderWidgetSettings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSettings_UpdatedByUserId",
                table: "StockReminderWidgetSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSubmissions_CompanyId",
                table: "StockReminderWidgetSubmissions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSubmissions_CreatedAt",
                table: "StockReminderWidgetSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockReminderWidgetSubmissions_StockReminderWidgetSettingsId",
                table: "StockReminderWidgetSubmissions",
                column: "StockReminderWidgetSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockReminderWidgetSubmissions");

            migrationBuilder.DropTable(
                name: "StockReminderWidgetSettings");

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppPermission",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
