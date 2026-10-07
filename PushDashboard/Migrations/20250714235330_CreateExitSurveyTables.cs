using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class CreateExitSurveyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExitSurveys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SubmitButtonText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CancelButtonText = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThankYouMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmitButtonColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CancelButtonColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorderRadius = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EnableAnimation = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnPageExit = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnTabClose = table.Column<bool>(type: "boolean", nullable: false),
                    DelayBeforeShow = table.Column<int>(type: "integer", nullable: false),
                    ShowFrequencyDays = table.Column<int>(type: "integer", nullable: false),
                    ShowOnMobile = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnDesktop = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExitSurveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExitSurveys_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExitSurveys_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExitSurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExitSurveyId = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QuestionType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OptionsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExitSurveyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExitSurveyQuestions_ExitSurveys_ExitSurveyId",
                        column: x => x.ExitSurveyId,
                        principalTable: "ExitSurveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExitSurveyResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExitSurveyId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    ResponseText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RatingValue = table.Column<int>(type: "integer", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExitSurveyResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExitSurveyResponses_ExitSurveyQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "ExitSurveyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExitSurveyResponses_ExitSurveys_ExitSurveyId",
                        column: x => x.ExitSurveyId,
                        principalTable: "ExitSurveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyQuestions_ExitSurveyId",
                table: "ExitSurveyQuestions",
                column: "ExitSurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyQuestions_IsActive",
                table: "ExitSurveyQuestions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyQuestions_SortOrder",
                table: "ExitSurveyQuestions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyResponses_CreatedAt",
                table: "ExitSurveyResponses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyResponses_ExitSurveyId",
                table: "ExitSurveyResponses",
                column: "ExitSurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyResponses_IpAddress",
                table: "ExitSurveyResponses",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyResponses_QuestionId",
                table: "ExitSurveyResponses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveyResponses_SessionId",
                table: "ExitSurveyResponses",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveys_CompanyId",
                table: "ExitSurveys",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveys_IsActive",
                table: "ExitSurveys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveys_UpdatedByUserId",
                table: "ExitSurveys",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExitSurveyResponses");

            migrationBuilder.DropTable(
                name: "ExitSurveyQuestions");

            migrationBuilder.DropTable(
                name: "ExitSurveys");
        }
    }
}
