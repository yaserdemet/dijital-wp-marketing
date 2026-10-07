using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class CreateCookieManagementTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CookieManagements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BannerTitle = table.Column<string>(type: "text", nullable: false),
                    BannerDescription = table.Column<string>(type: "text", nullable: false),
                    AcceptButtonText = table.Column<string>(type: "text", nullable: false),
                    RejectButtonText = table.Column<string>(type: "text", nullable: false),
                    SettingsButtonText = table.Column<string>(type: "text", nullable: false),
                    SaveButtonText = table.Column<string>(type: "text", nullable: false),
                    BannerPosition = table.Column<string>(type: "text", nullable: false),
                    BannerBackgroundColor = table.Column<string>(type: "text", nullable: false),
                    BannerTextColor = table.Column<string>(type: "text", nullable: false),
                    AcceptButtonColor = table.Column<string>(type: "text", nullable: false),
                    RejectButtonColor = table.Column<string>(type: "text", nullable: false),
                    SettingsButtonColor = table.Column<string>(type: "text", nullable: false),
                    BorderRadius = table.Column<string>(type: "text", nullable: false),
                    ShowSettingsButton = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAnimation = table.Column<bool>(type: "boolean", nullable: false),
                    CategoriesJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CookieExpiryDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookieManagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CookieManagements_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CookieManagements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });



            migrationBuilder.CreateIndex(
                name: "IX_CookieManagements_CompanyId",
                table: "CookieManagements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CookieManagements_UpdatedByUserId",
                table: "CookieManagements",
                column: "UpdatedByUserId");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CookieManagements");
        }
    }
}
