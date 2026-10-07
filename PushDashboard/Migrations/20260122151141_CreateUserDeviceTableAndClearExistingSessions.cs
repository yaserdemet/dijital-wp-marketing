using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserDeviceTableAndClearExistingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Browser",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "UserSessions");

            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "UserSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstLoginAt",
                table: "UserSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BrowserFamily = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    LastLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocalhost = table.Column<bool>(type: "boolean", nullable: false),
                    IsIncognito = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_DeviceId",
                table: "UserSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_DeviceFingerprint",
                table: "UserDevices",
                column: "DeviceFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_IsIncognito",
                table: "UserDevices",
                column: "IsIncognito");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_IsLocalhost",
                table: "UserDevices",
                column: "IsLocalhost");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId_DeviceFingerprint",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceFingerprint" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_UserDevices_DeviceId",
                table: "UserSessions",
                column: "DeviceId",
                principalTable: "UserDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_UserDevices_DeviceId",
                table: "UserSessions");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropIndex(
                name: "IX_UserSessions_DeviceId",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "FirstLoginAt",
                table: "UserSessions");

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "UserSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "UserSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "UserSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserSessions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "UserSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "UserSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "UserSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
