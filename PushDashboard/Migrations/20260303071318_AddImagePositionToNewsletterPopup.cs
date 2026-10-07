using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePositionToNewsletterPopup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePosition",
                table: "NewsletterPopupSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "left");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePosition",
                table: "NewsletterPopupSettings");
        }
    }
}
