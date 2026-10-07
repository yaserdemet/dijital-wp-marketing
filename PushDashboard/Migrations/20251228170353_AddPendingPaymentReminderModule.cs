using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingPaymentReminderModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingPaymentReminderSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReminderTimeForHours = table.Column<int>(type: "integer", nullable: false),
                    NotificationContent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CommunicationChannels = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "[]"),
                    ChannelMessages = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPaymentReminderSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingPaymentReminderSchedules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingPaymentReminderSchedules_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingPaymentReminderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxNotificationsPerOrder = table.Column<int>(type: "integer", nullable: false),
                    NotificationContent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPaymentReminderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingPaymentReminderSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingPaymentReminderSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingPaymentReminderLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "TRY"),
                    ReminderTimeForHours = table.Column<int>(type: "integer", nullable: false),
                    PendingPaymentReminderScheduleId = table.Column<int>(type: "integer", nullable: true),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NotificationContent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPaymentReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingPaymentReminderLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingPaymentReminderLogs_PendingPaymentReminderSchedules_~",
                        column: x => x.PendingPaymentReminderScheduleId,
                        principalTable: "PendingPaymentReminderSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderLogs_CompanyId",
                table: "PendingPaymentReminderLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderLogs_CompanyId_OrderExternalId_Remind~",
                table: "PendingPaymentReminderLogs",
                columns: new[] { "CompanyId", "OrderExternalId", "ReminderTimeForHours" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderLogs_OrderExternalId",
                table: "PendingPaymentReminderLogs",
                column: "OrderExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderLogs_PendingPaymentReminderScheduleId",
                table: "PendingPaymentReminderLogs",
                column: "PendingPaymentReminderScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderLogs_SentAt",
                table: "PendingPaymentReminderLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderSchedules_CompanyId",
                table: "PendingPaymentReminderSchedules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderSchedules_CompanyId_ReminderTimeForHo~",
                table: "PendingPaymentReminderSchedules",
                columns: new[] { "CompanyId", "ReminderTimeForHours" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderSchedules_CreatedByUserId",
                table: "PendingPaymentReminderSchedules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderSettings_CompanyId",
                table: "PendingPaymentReminderSettings",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentReminderSettings_UpdatedByUserId",
                table: "PendingPaymentReminderSettings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingPaymentReminderLogs");

            migrationBuilder.DropTable(
                name: "PendingPaymentReminderSettings");

            migrationBuilder.DropTable(
                name: "PendingPaymentReminderSchedules");
        }
    }
}
