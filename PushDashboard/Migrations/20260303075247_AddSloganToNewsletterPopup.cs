using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddSloganToNewsletterPopup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SloganBadgeText",
                table: "NewsletterPopupSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SloganEnabled",
                table: "NewsletterPopupSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SloganHighlightColor",
                table: "NewsletterPopupSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#c8a96e");

            migrationBuilder.AddColumn<string>(
                name: "SloganText",
                table: "NewsletterPopupSettings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SloganTextColor",
                table: "NewsletterPopupSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#ffffff");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SloganBadgeText",
                table: "NewsletterPopupSettings");

            migrationBuilder.DropColumn(
                name: "SloganEnabled",
                table: "NewsletterPopupSettings");

            migrationBuilder.DropColumn(
                name: "SloganHighlightColor",
                table: "NewsletterPopupSettings");

            migrationBuilder.DropColumn(
                name: "SloganText",
                table: "NewsletterPopupSettings");

            migrationBuilder.DropColumn(
                name: "SloganTextColor",
                table: "NewsletterPopupSettings");
        }
    }
}
