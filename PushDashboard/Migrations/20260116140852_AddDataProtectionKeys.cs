using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddDataProtectionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanySmsTemplates_CompanyId_SmsTemplateId",
                table: "CompanySmsTemplates");

            migrationBuilder.AlterColumn<int>(
                name: "SmsTemplateId",
                table: "CompanySmsTemplates",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySmsTemplates_CompanyId_SmsTemplateId",
                table: "CompanySmsTemplates",
                columns: new[] { "CompanyId", "SmsTemplateId" },
                unique: true,
                filter: "\"SmsTemplateId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropIndex(
                name: "IX_CompanySmsTemplates_CompanyId_SmsTemplateId",
                table: "CompanySmsTemplates");

            migrationBuilder.AlterColumn<int>(
                name: "SmsTemplateId",
                table: "CompanySmsTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanySmsTemplates_CompanyId_SmsTemplateId",
                table: "CompanySmsTemplates",
                columns: new[] { "CompanyId", "SmsTemplateId" },
                unique: true,
                filter: "[SmsTemplateId] != 0");
        }
    }
}
