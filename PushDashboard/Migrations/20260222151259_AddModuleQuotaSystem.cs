using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleQuotaSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasPackages",
                table: "Modules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasQuotaSystem",
                table: "Modules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ModulePackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    QuotaAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModulePackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModulePackages_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyModuleQuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<int>(type: "integer", nullable: false),
                    ModulePackageId = table.Column<int>(type: "integer", nullable: true),
                    TotalQuota = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RemainingQuota = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyModuleQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyModuleQuotas_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyModuleQuotas_ModulePackages_ModulePackageId",
                        column: x => x.ModulePackageId,
                        principalTable: "ModulePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanyModuleQuotas_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModuleQuotas_CompanyId_ModuleId",
                table: "CompanyModuleQuotas",
                columns: new[] { "CompanyId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModuleQuotas_ModuleId",
                table: "CompanyModuleQuotas",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModuleQuotas_ModulePackageId",
                table: "CompanyModuleQuotas",
                column: "ModulePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ModulePackages_ModuleId",
                table: "ModulePackages",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyModuleQuotas");

            migrationBuilder.DropTable(
                name: "ModulePackages");

            migrationBuilder.DropColumn(
                name: "HasPackages",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "HasQuotaSystem",
                table: "Modules");
        }
    }
}
