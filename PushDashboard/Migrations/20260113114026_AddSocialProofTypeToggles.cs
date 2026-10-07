using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialProofTypeToggles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBuyersActive",
                table: "SocialProofSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFollowersActive",
                table: "SocialProofSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsViewersActive",
                table: "SocialProofSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBuyersActive",
                table: "SocialProofSettings");

            migrationBuilder.DropColumn(
                name: "IsFollowersActive",
                table: "SocialProofSettings");

            migrationBuilder.DropColumn(
                name: "IsViewersActive",
                table: "SocialProofSettings");
        }
    }
}
