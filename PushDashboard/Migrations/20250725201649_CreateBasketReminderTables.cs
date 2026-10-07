using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class CreateBasketReminderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create BasketReminder Tables
            migrationBuilder.CreateTable(
                name: "BasketReminderSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReminderTimeForHours = table.Column<int>(type: "integer", nullable: false),
                    NotificationContent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CommunicationChannels = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "[]"),
                    ChannelMessages = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasketReminderSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BasketReminderSchedules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BasketReminderSchedules_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BasketReminderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxNotificationsPerCustomer = table.Column<int>(type: "integer", nullable: false),
                    NotificationContent = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasketReminderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BasketReminderSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BasketReminderSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CookieManagements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BannerTitle = table.Column<string>(type: "text", nullable: false),
                    BannerDescription = table.Column<string>(type: "text", nullable: false),
                    AcceptButtonText = table.Column<string>(type: "text", nullable: false),
                    RejectButtonText = table.Column<string>(type: "text", nullable: false),
                    SettingsButtonText = table.Column<string>(type: "text", nullable: false),
                    SaveButtonText = table.Column<string>(type: "text", nullable: false),
                    BannerPosition = table.Column<string>(type: "text", nullable: false),
                    BannerBackgroundColor = table.Column<string>(type: "text", nullable: false),
                    BannerTextColor = table.Column<string>(type: "text", nullable: false),
                    AcceptButtonColor = table.Column<string>(type: "text", nullable: false),
                    RejectButtonColor = table.Column<string>(type: "text", nullable: false),
                    SettingsButtonColor = table.Column<string>(type: "text", nullable: false),
                    BorderRadius = table.Column<string>(type: "text", nullable: false),
                    ShowSettingsButton = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAnimation = table.Column<bool>(type: "boolean", nullable: false),
                    CategoriesJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CookieExpiryDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookieManagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CookieManagements_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CookieManagements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "ProductSliders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSliders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSliders_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSliders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BasketReminderLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BasketId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasketExternalId = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerEmail = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    ReminderTimeForHours = table.Column<int>(type: "integer", nullable: false),
                    BasketReminderScheduleId = table.Column<int>(type: "integer", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    NotificationContent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasketReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BasketReminderLogs_BasketReminderSchedules_BasketReminderSc~",
                        column: x => x.BasketReminderScheduleId,
                        principalTable: "BasketReminderSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BasketReminderLogs_Baskets_BasketId",
                        column: x => x.BasketId,
                        principalTable: "Baskets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BasketReminderLogs_Companies_CompanyId",
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
                name: "ProductSliderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SliderId = table.Column<int>(type: "integer", nullable: false),
                    ProductTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductImage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProductUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProductPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSliderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSliderItems_ProductSliders_SliderId",
                        column: x => x.SliderId,
                        principalTable: "ProductSliders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSliderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SliderId = table.Column<int>(type: "integer", nullable: false),
                    AutoPlay = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPlayInterval = table.Column<int>(type: "integer", nullable: false),
                    ShowArrows = table.Column<bool>(type: "boolean", nullable: false),
                    ShowDots = table.Column<bool>(type: "boolean", nullable: false),
                    ItemsPerView = table.Column<int>(type: "integer", nullable: false),
                    ItemsPerViewMobile = table.Column<int>(type: "integer", nullable: false),
                    ItemsPerViewTablet = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EnableAnimations = table.Column<bool>(type: "boolean", nullable: false),
                    TransitionDuration = table.Column<int>(type: "integer", nullable: false),
                    AnimationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShowProductPrice = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProductDescription = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProductImage = table.Column<bool>(type: "boolean", nullable: false),
                    ImageAspectRatio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomCSS = table.Column<string>(type: "text", nullable: true),
                    ResponsiveBreakpoints = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSliderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSliderSettings_ProductSliders_SliderId",
                        column: x => x.SliderId,
                        principalTable: "ProductSliders",
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
                name: "IX_BasketReminderLogs_BasketId",
                table: "BasketReminderLogs",
                column: "BasketId");

            migrationBuilder.CreateIndex(
                name: "IX_BasketReminderLogs_BasketReminderScheduleId",
                table: "BasketReminderLogs",
                column: "BasketReminderScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BasketReminderLogs_CompanyId",
                table: "BasketReminderLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BasketReminderSchedules_CompanyId",
                table: "BasketReminderSchedules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BasketReminderSchedules_CompanyId_IsActive",
                table: "BasketReminderSchedules",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BasketReminderSchedules_CreatedByUserId",
                table: "BasketReminderSchedules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BasketReminderSettings_CompanyId",
                table: "BasketReminderSettings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BasketReminderSettings_UpdatedByUserId",
                table: "BasketReminderSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CookieManagements_CompanyId",
                table: "CookieManagements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CookieManagements_UpdatedByUserId",
                table: "CookieManagements",
                column: "UpdatedByUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ProductSliderItems_SliderId",
                table: "ProductSliderItems",
                column: "SliderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSliders_CompanyId",
                table: "ProductSliders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSliders_CreatedByUserId",
                table: "ProductSliders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSliderSettings_SliderId",
                table: "ProductSliderSettings",
                column: "SliderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasketReminderLogs");

            migrationBuilder.DropTable(
                name: "BasketReminderSettings");

            migrationBuilder.DropTable(
                name: "CookieManagements");

            migrationBuilder.DropTable(
                name: "ExitSurveyResponses");

            migrationBuilder.DropTable(
                name: "ProductSliderItems");

            migrationBuilder.DropTable(
                name: "ProductSliderSettings");

            migrationBuilder.DropTable(
                name: "BasketReminderSchedules");

            migrationBuilder.DropTable(
                name: "ExitSurveyQuestions");

            migrationBuilder.DropTable(
                name: "ProductSliders");

            migrationBuilder.DropTable(
                name: "ExitSurveys");
        }
    }
}
