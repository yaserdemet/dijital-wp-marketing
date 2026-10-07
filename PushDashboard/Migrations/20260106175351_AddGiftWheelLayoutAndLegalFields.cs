using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftWheelLayoutAndLegalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Layout",
                table: "GiftWheelSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyUrl",
                table: "GiftWheelSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsUrl",
                table: "GiftWheelSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Layout",
                table: "GiftWheelSettings");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyUrl",
                table: "GiftWheelSettings");

            migrationBuilder.DropColumn(
                name: "TermsUrl",
                table: "GiftWheelSettings");
        }
    }
}
