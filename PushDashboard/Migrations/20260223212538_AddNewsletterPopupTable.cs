using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterPopupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsletterPopupSettings",
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
                    NamePlaceholder = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SurnamePlaceholder = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhonePlaceholder = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ButtonColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ButtonTextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorderRadius = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShowNameFields = table.Column<bool>(type: "boolean", nullable: false),
                    ShowPhoneField = table.Column<bool>(type: "boolean", nullable: false),
                    ShowConsentCheckbox = table.Column<bool>(type: "boolean", nullable: false),
                    DelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    ShowFrequencyDays = table.Column<int>(type: "integer", nullable: false),
                    ShowOnMobile = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnDesktop = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAnimation = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ImpressionCount = table.Column<int>(type: "integer", nullable: false),
                    SubmitCount = table.Column<int>(type: "integer", nullable: false),
                    DismissCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterPopupSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsletterPopupSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewsletterPopupSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterPopupEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NewsletterPopupSettingsId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsMobile = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerSurname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterPopupEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsletterPopupEvents_NewsletterPopupSettings_NewsletterPop~",
                        column: x => x.NewsletterPopupSettingsId,
                        principalTable: "NewsletterPopupSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupEvents_CreatedAt",
                table: "NewsletterPopupEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupEvents_EventType",
                table: "NewsletterPopupEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupEvents_IsMobile",
                table: "NewsletterPopupEvents",
                column: "IsMobile");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupEvents_NewsletterPopupSettingsId",
                table: "NewsletterPopupEvents",
                column: "NewsletterPopupSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupEvents_SessionId",
                table: "NewsletterPopupEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupSettings_CompanyId",
                table: "NewsletterPopupSettings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupSettings_IsActive",
                table: "NewsletterPopupSettings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterPopupSettings_UpdatedByUserId",
                table: "NewsletterPopupSettings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsletterPopupEvents");

            migrationBuilder.DropTable(
                name: "NewsletterPopupSettings");
        }
    }
}
