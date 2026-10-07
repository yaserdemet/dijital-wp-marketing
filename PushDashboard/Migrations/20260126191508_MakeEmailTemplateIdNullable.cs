using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmailTemplateIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyEmailTemplates_EmailTemplates_EmailTemplateId",
                table: "CompanyEmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CompanyEmailTemplates_CompanyId_EmailTemplateId",
                table: "CompanyEmailTemplates");

            migrationBuilder.AlterColumn<int>(
                name: "EmailTemplateId",
                table: "CompanyEmailTemplates",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmailTemplates_CompanyId_EmailTemplateId",
                table: "CompanyEmailTemplates",
                columns: new[] { "CompanyId", "EmailTemplateId" },
                unique: true,
                filter: "\"EmailTemplateId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyEmailTemplates_EmailTemplates_EmailTemplateId",
                table: "CompanyEmailTemplates",
                column: "EmailTemplateId",
                principalTable: "EmailTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyEmailTemplates_EmailTemplates_EmailTemplateId",
                table: "CompanyEmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CompanyEmailTemplates_CompanyId_EmailTemplateId",
                table: "CompanyEmailTemplates");

            migrationBuilder.AlterColumn<int>(
                name: "EmailTemplateId",
                table: "CompanyEmailTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmailTemplates_CompanyId_EmailTemplateId",
                table: "CompanyEmailTemplates",
                columns: new[] { "CompanyId", "EmailTemplateId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyEmailTemplates_EmailTemplates_EmailTemplateId",
                table: "CompanyEmailTemplates",
                column: "EmailTemplateId",
                principalTable: "EmailTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
