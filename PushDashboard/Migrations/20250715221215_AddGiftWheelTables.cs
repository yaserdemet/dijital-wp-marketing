using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftWheelTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiftWheels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftWheels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftWheels_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftWheelPrizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftWheelId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrizeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    DiscountType = table.Column<int>(type: "integer", nullable: true),
                    ValidityDays = table.Column<int>(type: "integer", nullable: true),
                    Probability = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftWheelPrizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftWheelPrizes_GiftWheels_GiftWheelId",
                        column: x => x.GiftWheelId,
                        principalTable: "GiftWheels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftWheelSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftWheelId = table.Column<int>(type: "integer", nullable: false),
                    WheelTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WheelSubtitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ButtonText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WinMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LoseMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxSpinsPerDay = table.Column<int>(type: "integer", nullable: false),
                    RequirePhone = table.Column<bool>(type: "boolean", nullable: false),
                    RequireEmail = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    WheelSize = table.Column<int>(type: "integer", nullable: false),
                    ShowConfetti = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftWheelSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftWheelSettings_GiftWheels_GiftWheelId",
                        column: x => x.GiftWheelId,
                        principalTable: "GiftWheels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftWheelSpins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftWheelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PrizeId = table.Column<int>(type: "integer", nullable: false),
                    VoucherCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VoucherCreated = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    SpinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftWheelSpins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftWheelSpins_GiftWheelPrizes_PrizeId",
                        column: x => x.PrizeId,
                        principalTable: "GiftWheelPrizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiftWheelSpins_GiftWheels_GiftWheelId",
                        column: x => x.GiftWheelId,
                        principalTable: "GiftWheels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiftWheelPrizes_GiftWheelId",
                table: "GiftWheelPrizes",
                column: "GiftWheelId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftWheels_CompanyId",
                table: "GiftWheels",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftWheelSettings_GiftWheelId",
                table: "GiftWheelSettings",
                column: "GiftWheelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftWheelSpins_GiftWheelId",
                table: "GiftWheelSpins",
                column: "GiftWheelId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftWheelSpins_PrizeId",
                table: "GiftWheelSpins",
                column: "PrizeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiftWheelSettings");

            migrationBuilder.DropTable(
                name: "GiftWheelSpins");

            migrationBuilder.DropTable(
                name: "GiftWheelPrizes");

            migrationBuilder.DropTable(
                name: "GiftWheels");
        }
    }
}
