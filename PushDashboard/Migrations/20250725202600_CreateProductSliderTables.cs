using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductSliderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ProductSliders table
            migrationBuilder.CreateTable(
                name: "ProductSliders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSliders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSliders_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSliders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create ProductSliderItems table
            migrationBuilder.CreateTable(
                name: "ProductSliderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SliderId = table.Column<int>(type: "integer", nullable: false),
                    ProductTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductImage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProductUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProductPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ProductDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSliderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSliderItems_ProductSliders_SliderId",
                        column: x => x.SliderId,
                        principalTable: "ProductSliders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create ProductSliderSettings table
            migrationBuilder.CreateTable(
                name: "ProductSliderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SliderId = table.Column<int>(type: "integer", nullable: false),
                    AutoPlay = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPlayInterval = table.Column<int>(type: "integer", nullable: false),
                    ShowArrows = table.Column<bool>(type: "boolean", nullable: false),
                    ShowDots = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProductImage = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProductPrice = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProductDescription = table.Column<bool>(type: "boolean", nullable: false),
                    ItemsPerSlide = table.Column<int>(type: "integer", nullable: false),
                    TransitionDuration = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorderRadius = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSliderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSliderSettings_ProductSliders_SliderId",
                        column: x => x.SliderId,
                        principalTable: "ProductSliders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ProductSliderItems_SliderId",
                table: "ProductSliderItems",
                column: "SliderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSliders_CompanyId",
                table: "ProductSliders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSliders_CreatedByUserId",
                table: "ProductSliders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSliderSettings_SliderId",
                table: "ProductSliderSettings",
                column: "SliderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSliderItems");

            migrationBuilder.DropTable(
                name: "ProductSliderSettings");

            migrationBuilder.DropTable(
                name: "ProductSliders");
        }
    }
}
