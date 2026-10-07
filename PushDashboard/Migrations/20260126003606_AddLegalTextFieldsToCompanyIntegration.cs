using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalTextFieldsToCompanyIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KvkkLink",
                table: "CompanyIntegrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "LegalTextsAccepted",
                table: "CompanyIntegrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TicariElektronikIletiLink",
                table: "CompanyIntegrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KvkkLink",
                table: "CompanyIntegrations");

            migrationBuilder.DropColumn(
                name: "LegalTextsAccepted",
                table: "CompanyIntegrations");

            migrationBuilder.DropColumn(
                name: "TicariElektronikIletiLink",
                table: "CompanyIntegrations");
        }
    }
}
