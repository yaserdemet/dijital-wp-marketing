using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentTransferJobProducts_TrendyolProducts_ProductId",
                table: "CommentTransferJobProducts");

            migrationBuilder.DropIndex(
                name: "IX_CommentTransferJobProducts_JobId_ProductId_Unique",
                table: "CommentTransferJobProducts");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CommentTransferJobProducts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobProducts_JobId_ProductId",
                table: "CommentTransferJobProducts",
                columns: new[] { "JobId", "ProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CommentTransferJobProducts_TrendyolProducts_ProductId",
                table: "CommentTransferJobProducts",
                column: "ProductId",
                principalTable: "TrendyolProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentTransferJobProducts_TrendyolProducts_ProductId",
                table: "CommentTransferJobProducts");

            migrationBuilder.DropIndex(
                name: "IX_CommentTransferJobProducts_JobId_ProductId",
                table: "CommentTransferJobProducts");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CommentTransferJobProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobProducts_JobId_ProductId_Unique",
                table: "CommentTransferJobProducts",
                columns: new[] { "JobId", "ProductId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentTransferJobProducts_TrendyolProducts_ProductId",
                table: "CommentTransferJobProducts",
                column: "ProductId",
                principalTable: "TrendyolProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
