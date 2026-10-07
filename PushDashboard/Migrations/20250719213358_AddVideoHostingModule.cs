using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoHostingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoHostings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    S3Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    S3Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ThumbnailS3Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThumbnailS3Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    VideoCodec = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AudioCodec = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FrameRate = table.Column<double>(type: "double precision", nullable: false),
                    UploadCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StorageCostPerMonth = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessingError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoHostings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoHostings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoHostings_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoHostings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoHostingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostPerMinute = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0.5m),
                    StorageCostPerGBPerMonth = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0.1m),
                    MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 524288000L),
                    MaxDurationMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    MaxVideosPerCompany = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    AutoPlay = table.Column<bool>(type: "boolean", nullable: false),
                    ShowControls = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDownload = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerTheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "default"),
                    CustomPlayerCss = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    RequireAuthentication = table.Column<bool>(type: "boolean", nullable: false),
                    AllowEmbedding = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedDomains = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoHostingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoHostingSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoHostingSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoHostingViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VideoHostingId = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewerIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    ViewerUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ViewerCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ViewerCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    WatchDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CompletedView = table.Column<bool>(type: "boolean", nullable: false),
                    ReferrerUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmbedUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoHostingViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoHostingViews_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoHostingViews_VideoHostings_VideoHostingId",
                        column: x => x.VideoHostingId,
                        principalTable: "VideoHostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostings_AccessToken",
                table: "VideoHostings",
                column: "AccessToken");

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostings_CompanyId_Status",
                table: "VideoHostings",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostings_CreatedAt",
                table: "VideoHostings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostings_S3Key",
                table: "VideoHostings",
                column: "S3Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostings_UpdatedByUserId",
                table: "VideoHostings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostings_UploadedByUserId",
                table: "VideoHostings",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostingSettings_CompanyId",
                table: "VideoHostingSettings",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostingSettings_UpdatedByUserId",
                table: "VideoHostingSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostingViews_CompanyId_ViewedAt",
                table: "VideoHostingViews",
                columns: new[] { "CompanyId", "ViewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostingViews_VideoHostingId",
                table: "VideoHostingViews",
                column: "VideoHostingId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoHostingViews_ViewerIpAddress",
                table: "VideoHostingViews",
                column: "ViewerIpAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoHostingSettings");

            migrationBuilder.DropTable(
                name: "VideoHostingViews");

            migrationBuilder.DropTable(
                name: "VideoHostings");
        }
    }
}
