using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToTemplateNameBasedMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WhatsAppTemplateVariableMappings_CompanyId_TemplateId",
                table: "WhatsAppTemplateVariableMappings");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppTemplateVariableMappings_Unique",
                table: "WhatsAppTemplateVariableMappings");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppTemplateSettings_CompanyId_TemplateId",
                table: "WhatsAppTemplateSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppTemplateId",
                table: "WhatsAppTemplateVariableMappings");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "WhatsAppTemplateSettings");

            migrationBuilder.AlterColumn<string>(
                name: "VariableType",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "VariableLabel",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ComponentType",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "BODY");

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppTemplateName",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateVariableMapping_Unique",
                table: "WhatsAppTemplateVariableMappings",
                columns: new[] { "CompanyId", "WhatsAppTemplateName", "ComponentType", "VariablePosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateVariableMappings_CompanyId_TemplateName",
                table: "WhatsAppTemplateVariableMappings",
                columns: new[] { "CompanyId", "WhatsAppTemplateName" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateSettings_CompanyId_TemplateName",
                table: "WhatsAppTemplateSettings",
                columns: new[] { "CompanyId", "TemplateName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WhatsAppTemplateVariableMapping_Unique",
                table: "WhatsAppTemplateVariableMappings");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppTemplateVariableMappings_CompanyId_TemplateName",
                table: "WhatsAppTemplateVariableMappings");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppTemplateSettings_CompanyId_TemplateName",
                table: "WhatsAppTemplateSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppTemplateName",
                table: "WhatsAppTemplateVariableMappings");

            migrationBuilder.AlterColumn<string>(
                name: "VariableType",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "VariableLabel",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ComponentType",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "BODY",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppTemplateId",
                table: "WhatsAppTemplateVariableMappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateId",
                table: "WhatsAppTemplateSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateVariableMappings_CompanyId_TemplateId",
                table: "WhatsAppTemplateVariableMappings",
                columns: new[] { "CompanyId", "WhatsAppTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateVariableMappings_Unique",
                table: "WhatsAppTemplateVariableMappings",
                columns: new[] { "CompanyId", "WhatsAppTemplateId", "ComponentType", "VariablePosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplateSettings_CompanyId_TemplateId",
                table: "WhatsAppTemplateSettings",
                columns: new[] { "CompanyId", "TemplateId" },
                unique: true);
        }
    }
}
