using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleTypeToCompanySmsTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModuleType",
                table: "CompanySmsTemplates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModuleType",
                table: "CompanySmsTemplates");
        }
    }
}
