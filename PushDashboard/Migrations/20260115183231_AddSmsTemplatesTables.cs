using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsTemplatesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiftVoucherUsed",
                table: "FirstOrderCustomers");

            migrationBuilder.DropColumn(
                name: "GiftVoucherUsedAt",
                table: "FirstOrderCustomers");

            migrationBuilder.CreateTable(
                name: "SmsTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultContent = table.Column<string>(type: "text", nullable: false),
                    Variables = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ModuleId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanySmsTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SmsTemplateId = table.Column<int>(type: "integer", nullable: true),
                    CustomContent = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySmsTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySmsTemplates_AspNetUsers_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanySmsTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanySmsTemplates_SmsTemplates_SmsTemplateId",
                        column: x => x.SmsTemplateId,
                        principalTable: "SmsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySmsTemplates_CompanyId",
                table: "CompanySmsTemplates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySmsTemplates_CompanyId_SmsTemplateId",
                table: "CompanySmsTemplates",
                columns: new[] { "CompanyId", "SmsTemplateId" },
                unique: true,
                filter: "\"SmsTemplateId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySmsTemplates_LastModifiedBy",
                table: "CompanySmsTemplates",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySmsTemplates_SmsTemplateId",
                table: "CompanySmsTemplates",
                column: "SmsTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplates_Category",
                table: "SmsTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplates_IsActive_Category",
                table: "SmsTemplates",
                columns: new[] { "IsActive", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySmsTemplates");

            migrationBuilder.DropTable(
                name: "SmsTemplates");

            migrationBuilder.AddColumn<bool>(
                name: "GiftVoucherUsed",
                table: "FirstOrderCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "GiftVoucherUsedAt",
                table: "FirstOrderCustomers",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
