using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialProofSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SocialProofSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ViewersMin = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    ViewersMax = table.Column<int>(type: "integer", nullable: false, defaultValue: 45),
                    FollowersMin = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    FollowersMax = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    BuyersMin = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    BuyersMax = table.Column<int>(type: "integer", nullable: false, defaultValue: 8),
                    UpdateInterval = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    DisplayDuration = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    TextTemplatesJson = table.Column<string>(type: "text", nullable: true),
                    DisplaySettingsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialProofSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialProofSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialProofSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialProofSettings_CompanyId_Unique",
                table: "SocialProofSettings",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialProofSettings_IsActive_Performance",
                table: "SocialProofSettings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SocialProofSettings_UpdatedAt_Performance",
                table: "SocialProofSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SocialProofSettings_UpdatedByUserId",
                table: "SocialProofSettings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialProofSettings");
        }
    }
}
