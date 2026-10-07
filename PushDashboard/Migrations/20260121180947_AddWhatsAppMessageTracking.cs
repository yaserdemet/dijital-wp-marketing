using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppMessageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "WhatsAppMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GupshupMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyIntegrationId = table.Column<int>(type: "integer", nullable: true),
                    RecipientPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TemplateId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MessageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "template"),
                    MessageContent = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceReferenceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_CompanyIntegrations_CompanyIntegrationId",
                        column: x => x.CompanyIntegrationId,
                        principalTable: "CompanyIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WhatsAppMessageId = table.Column<int>(type: "integer", nullable: true),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventSubType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GupshupMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GupshupTimestamp = table.Column<long>(type: "bigint", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SourceIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppWebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppWebhookEvents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhatsAppWebhookEvents_WhatsAppMessages_WhatsAppMessageId",
                        column: x => x.WhatsAppMessageId,
                        principalTable: "WhatsAppMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers",
                column: "ReferralCode",
                unique: true,
                filter: "\"ReferralCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_CompanyId",
                table: "WhatsAppMessages",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_CompanyId_SentAt",
                table: "WhatsAppMessages",
                columns: new[] { "CompanyId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_CompanyId_Status",
                table: "WhatsAppMessages",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_CompanyIntegrationId",
                table: "WhatsAppMessages",
                column: "CompanyIntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_GupshupMessageId",
                table: "WhatsAppMessages",
                column: "GupshupMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_SentAt",
                table: "WhatsAppMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_Status",
                table: "WhatsAppMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_CompanyId",
                table: "WhatsAppWebhookEvents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_CompanyId_ReceivedAt",
                table: "WhatsAppWebhookEvents",
                columns: new[] { "CompanyId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_GupshupMessageId",
                table: "WhatsAppWebhookEvents",
                column: "GupshupMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_IsProcessed",
                table: "WhatsAppWebhookEvents",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_ReceivedAt",
                table: "WhatsAppWebhookEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppWebhookEvents_WhatsAppMessageId",
                table: "WhatsAppWebhookEvents",
                column: "WhatsAppMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppWebhookEvents");

            migrationBuilder.DropTable(
                name: "WhatsAppMessages");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");
        }
    }
}
