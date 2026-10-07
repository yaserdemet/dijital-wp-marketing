using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileImageToNewsletterPopup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideImageOnMobile",
                table: "NewsletterPopupSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MobileImageUrl",
                table: "NewsletterPopupSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideImageOnMobile",
                table: "NewsletterPopupSettings");

            migrationBuilder.DropColumn(
                name: "MobileImageUrl",
                table: "NewsletterPopupSettings");
        }
    }
}
