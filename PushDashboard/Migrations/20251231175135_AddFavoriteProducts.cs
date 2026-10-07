using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalFavoriteId = table.Column<int>(type: "integer", nullable: false),
                    ExternalProductId = table.Column<int>(type: "integer", nullable: false),
                    ExternalMemberId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StockCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceWithVat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "TRY"),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProductUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VariantCount = table.Column<int>(type: "integer", nullable: false),
                    AddedToFavoritesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteProducts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteProducts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FavoriteProductSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    TotalFetched = table.Column<int>(type: "integer", nullable: false),
                    NewRecords = table.Column<int>(type: "integer", nullable: false),
                    UpdatedRecords = table.Column<int>(type: "integer", nullable: false),
                    DeletedRecords = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteProductSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteProductSyncLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_AddedToFavoritesAt",
                table: "FavoriteProducts",
                column: "AddedToFavoritesAt");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_CompanyId",
                table: "FavoriteProducts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_CompanyId_ExternalFavoriteId",
                table: "FavoriteProducts",
                columns: new[] { "CompanyId", "ExternalFavoriteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_CompanyId_ExternalProductId",
                table: "FavoriteProducts",
                columns: new[] { "CompanyId", "ExternalProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_CompanyId_IsActive",
                table: "FavoriteProducts",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_CustomerId",
                table: "FavoriteProducts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_ExternalFavoriteId",
                table: "FavoriteProducts",
                column: "ExternalFavoriteId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_ExternalMemberId",
                table: "FavoriteProducts",
                column: "ExternalMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_ExternalProductId",
                table: "FavoriteProducts",
                column: "ExternalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProductSyncLogs_CompanyId",
                table: "FavoriteProductSyncLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProductSyncLogs_CompanyId_StartedAt",
                table: "FavoriteProductSyncLogs",
                columns: new[] { "CompanyId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProductSyncLogs_StartedAt",
                table: "FavoriteProductSyncLogs",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteProducts");

            migrationBuilder.DropTable(
                name: "FavoriteProductSyncLogs");
        }
    }
}
