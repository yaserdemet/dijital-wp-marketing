using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppTemplateVariableMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhatsAppTemplateVariableMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WhatsAppTemplateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ComponentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "BODY"),
                    VariablePosition = table.Column<int>(type: "integer", nullable: false),
                    VariableType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VariableLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppTemplateVariableMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppTemplateVariableMappings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateVariableMappings_CompanyId",
                table: "WhatsAppTemplateVariableMappings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateVariableMappings_CompanyId_TemplateId",
                table: "WhatsAppTemplateVariableMappings",
                columns: new[] { "CompanyId", "WhatsAppTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateVariableMappings_Unique",
                table: "WhatsAppTemplateVariableMappings",
                columns: new[] { "CompanyId", "WhatsAppTemplateId", "ComponentType", "VariablePosition" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppTemplateVariableMappings");
        }
    }
}
