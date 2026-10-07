using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderStatusMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalStatusDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InternalStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusMappings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusMappings_Company_Integration_Active",
                table: "OrderStatusMappings",
                columns: new[] { "CompanyId", "IntegrationType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusMappings_Company_Integration_ExternalStatus",
                table: "OrderStatusMappings",
                columns: new[] { "CompanyId", "IntegrationType", "ExternalStatus" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderStatusMappings");
        }
    }
}
