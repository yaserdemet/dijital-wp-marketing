using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithGuidCompanyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TaxOffice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TaxNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IdentityNumber = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreditBalance = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DefaultContent = table.Column<string>(type: "text", nullable: false),
                    DefaultSubject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Variables = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ModuleId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetailedDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IconClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IconColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPopular = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Features = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefaultSettingsTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IconClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TwoFactorSecretKey = table.Column<string>(type: "text", nullable: true),
                    CreditBalance = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Baskets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuidBasketId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BasketDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ShippingCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsAbandoned = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baskets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Baskets_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExternalProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalProductUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedCommentsCount = table.Column<int>(type: "integer", nullable: false),
                    ActualCommentsCount = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CommentsFileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogsFileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScreenshotUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExportToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalRequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsProcessing = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalId = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MobilePhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailPermission = table.Column<bool>(type: "boolean", nullable: false),
                    SmsPermission = table.Column<bool>(type: "boolean", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GenderId = table.Column<int>(type: "integer", nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MembershipType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MembershipDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastUpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PointBalance = table.Column<int>(type: "integer", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    KvkkApproval = table.Column<bool>(type: "boolean", nullable: false),
                    MembershipAgreementApproval = table.Column<bool>(type: "boolean", nullable: false),
                    CashOnDeliveryBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    Profession = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EducationLevel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OldStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NewStatusDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OrderAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    OrderCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WebhookPayload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationChannels = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotificationError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusChangeLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderStatusDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailNotificationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "integer", nullable: true),
                    SmsNotificationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsTemplateId = table.Column<int>(type: "integer", nullable: true),
                    WhatsAppNotificationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppTemplateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NotificationOrder = table.Column<int>(type: "integer", nullable: false),
                    DelayMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastNotificationAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalNotificationsSent = table.Column<int>(type: "integer", nullable: false),
                    AdditionalSettings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusNotifications_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SyncStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalRecordsProcessed = table.Column<int>(type: "integer", nullable: false),
                    NewRecordsAdded = table.Column<int>(type: "integer", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "integer", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrendyolStores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalStoreId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StoreName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SyncStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrendyolStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrendyolStores_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsConfigured = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfiguredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettingsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SyncStatsJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyIntegrations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyIntegrations_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetailedDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IconClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IconColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsNew = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Features = table.Column<string>(type: "text", nullable: true),
                    DefaultSettings = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_ModuleCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ModuleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BulkMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CustomerFiltersJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ChannelSettingsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalRecipients = table.Column<int>(type: "integer", nullable: false),
                    ProcessedRecipients = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulSends = table.Column<int>(type: "integer", nullable: false),
                    FailedSends = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ActualCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CurrentBatch = table.Column<int>(type: "integer", nullable: false),
                    TotalBatches = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkMessages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BulkMessages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "integer", nullable: false),
                    CustomContent = table.Column<string>(type: "text", nullable: false),
                    CustomSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyEmailTemplates_AspNetUsers_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanyEmailTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyEmailTemplates_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerImportJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    ProcessedRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorDetails = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerImportJobs_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerImportJobs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EmailInvoiceNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EmailCreditNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EmailMarketingNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    SmsSecurityAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    SmsPaymentNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitationToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInvitations_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInvitations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BasketItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BasketId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<int>(type: "integer", nullable: false),
                    GuidBasketItemId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductImage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TaxRate = table.Column<double>(type: "double precision", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ShippingCost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FreeShipping = table.Column<bool>(type: "boolean", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasketItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BasketItems_Baskets_BasketId",
                        column: x => x.BasketId,
                        principalTable: "Baskets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentTransferJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    JobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalBatchId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JobType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    CurrentStep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalProducts = table.Column<int>(type: "integer", nullable: true),
                    ProcessedProducts = table.Column<int>(type: "integer", nullable: true),
                    TotalComments = table.Column<int>(type: "integer", nullable: true),
                    ResultFileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogsFileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentTransferJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentTransferJobs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentTransferJobs_TrendyolStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "TrendyolStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrendyolProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Href = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrendyolProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrendyolProducts_TrendyolStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "TrendyolStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PurchasedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyModules_AspNetUsers_PurchasedByUserId",
                        column: x => x.PurchasedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyModules_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleUsageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<int>(type: "integer", nullable: false),
                    UsageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleUsageLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleUsageLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleUsageLogs_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BulkMessageRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BulkMessageId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SentChannels = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkMessageRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkMessageRecipients_BulkMessages_BulkMessageId",
                        column: x => x.BulkMessageId,
                        principalTable: "BulkMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulkMessageRecipients_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentTransferJobProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentTransferJobProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentTransferJobProducts_CommentTransferJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "CommentTransferJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentTransferJobProducts_TrendyolProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TrendyolProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyModuleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyModuleId = table.Column<int>(type: "integer", nullable: false),
                    SettingsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyModuleSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyModuleSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyModuleSettings_CompanyModules_CompanyModuleId",
                        column: x => x.CompanyModuleId,
                        principalTable: "CompanyModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BasketItems_BasketId_Performance",
                table: "BasketItems",
                column: "BasketId");

            migrationBuilder.CreateIndex(
                name: "IX_Baskets_BasketDate_Performance",
                table: "Baskets",
                column: "BasketDate");

            migrationBuilder.CreateIndex(
                name: "IX_Baskets_CompanyId_Performance",
                table: "Baskets",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Baskets_ExternalId_CompanyId_Performance",
                table: "Baskets",
                columns: new[] { "ExternalId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageRecipients_BulkMessage_Status_Performance",
                table: "BulkMessageRecipients",
                columns: new[] { "BulkMessageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageRecipients_BulkMessageId_Performance",
                table: "BulkMessageRecipients",
                column: "BulkMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageRecipients_CustomerId",
                table: "BulkMessageRecipients",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageRecipients_Status_Performance",
                table: "BulkMessageRecipients",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessages_Company_Status_Date_Performance",
                table: "BulkMessages",
                columns: new[] { "CompanyId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessages_CompanyId_Performance",
                table: "BulkMessages",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessages_CreatedAt_Performance",
                table: "BulkMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessages_Status_Performance",
                table: "BulkMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessages_UserId",
                table: "BulkMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRequests_CompanyId_Performance",
                table: "CommentRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRequests_CreatedAt_Performance",
                table: "CommentRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRequests_Status_Performance",
                table: "CommentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobProducts_JobId_Performance",
                table: "CommentTransferJobProducts",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobProducts_JobId_ProductId_Unique",
                table: "CommentTransferJobProducts",
                columns: new[] { "JobId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobProducts_ProductId_Performance",
                table: "CommentTransferJobProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobProducts_Status_Performance",
                table: "CommentTransferJobProducts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobs_CompanyId_Performance",
                table: "CommentTransferJobs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobs_CreatedAt_Performance",
                table: "CommentTransferJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobs_JobId_Unique",
                table: "CommentTransferJobs",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobs_Status_Performance",
                table: "CommentTransferJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommentTransferJobs_StoreId_Performance",
                table: "CommentTransferJobs",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmailTemplates_CompanyId_EmailTemplateId",
                table: "CompanyEmailTemplates",
                columns: new[] { "CompanyId", "EmailTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmailTemplates_EmailTemplateId",
                table: "CompanyEmailTemplates",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmailTemplates_LastModifiedBy",
                table: "CompanyEmailTemplates",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyIntegrations_CompanyId_IntegrationId",
                table: "CompanyIntegrations",
                columns: new[] { "CompanyId", "IntegrationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyIntegrations_IntegrationId",
                table: "CompanyIntegrations",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModules_CompanyId_ModuleId",
                table: "CompanyModules",
                columns: new[] { "CompanyId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModules_ModuleId",
                table: "CompanyModules",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModules_PurchasedByUserId",
                table: "CompanyModules",
                column: "PurchasedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModuleSettings_CompanyModuleId",
                table: "CompanyModuleSettings",
                column: "CompanyModuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModuleSettings_UpdatedByUserId",
                table: "CompanyModuleSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerImportJobs_CompanyId_Performance",
                table: "CustomerImportJobs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerImportJobs_CreatedAt_Performance",
                table: "CustomerImportJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerImportJobs_CreatedByUserId",
                table: "CustomerImportJobs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerImportJobs_Status_Performance",
                table: "CustomerImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId",
                table: "Customers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ExternalId_CompanyId",
                table: "Customers",
                columns: new[] { "ExternalId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CategoryId",
                table: "Modules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleUsageLogs_Company_Module_Date_Performance",
                table: "ModuleUsageLogs",
                columns: new[] { "CompanyId", "ModuleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleUsageLogs_CompanyId_Performance",
                table: "ModuleUsageLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleUsageLogs_CreatedAt_Performance",
                table: "ModuleUsageLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleUsageLogs_ModuleId",
                table: "ModuleUsageLogs",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleUsageLogs_UserId",
                table: "ModuleUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChangeLogs_Company_Date_Performance",
                table: "OrderStatusChangeLogs",
                columns: new[] { "CompanyId", "StatusChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChangeLogs_Company_Status_Date_Performance",
                table: "OrderStatusChangeLogs",
                columns: new[] { "CompanyId", "NewStatus", "StatusChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChangeLogs_CompanyId_Performance",
                table: "OrderStatusChangeLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChangeLogs_OrderId_Performance",
                table: "OrderStatusChangeLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChangeLogs_StatusChangedAt_Performance",
                table: "OrderStatusChangeLogs",
                column: "StatusChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusNotifications_CompanyId_OrderStatus",
                table: "OrderStatusNotifications",
                columns: new[] { "CompanyId", "OrderStatus" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusNotifications_CompanyId_Performance",
                table: "OrderStatusNotifications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusNotifications_IsActive_Performance",
                table: "OrderStatusNotifications",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_CompanyId",
                table: "SyncLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TrendyolProducts_IsSelected_Performance",
                table: "TrendyolProducts",
                column: "IsSelected");

            migrationBuilder.CreateIndex(
                name: "IX_TrendyolProducts_ProductId_Performance",
                table: "TrendyolProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TrendyolProducts_StoreId_Performance",
                table: "TrendyolProducts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_TrendyolProducts_StoreId_ProductId_Unique",
                table: "TrendyolProducts",
                columns: new[] { "StoreId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrendyolStores_CompanyId_Performance",
                table: "TrendyolStores",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TrendyolStores_CompanyId_StoreUrl_Unique",
                table: "TrendyolStores",
                columns: new[] { "CompanyId", "StoreUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrendyolStores_SyncStatus_Performance",
                table: "TrendyolStores",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_CompanyId",
                table: "UserInvitations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_CreatedBy",
                table: "UserInvitations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_Email_CompanyId",
                table: "UserInvitations",
                columns: new[] { "Email", "CompanyId" },
                unique: true,
                filter: "\"IsUsed\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_InvitationToken",
                table: "UserInvitations",
                column: "InvitationToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BasketItems");

            migrationBuilder.DropTable(
                name: "BulkMessageRecipients");

            migrationBuilder.DropTable(
                name: "CommentRequests");

            migrationBuilder.DropTable(
                name: "CommentTransferJobProducts");

            migrationBuilder.DropTable(
                name: "CompanyEmailTemplates");

            migrationBuilder.DropTable(
                name: "CompanyIntegrations");

            migrationBuilder.DropTable(
                name: "CompanyModuleSettings");

            migrationBuilder.DropTable(
                name: "CustomerImportJobs");

            migrationBuilder.DropTable(
                name: "ModuleUsageLogs");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "OrderStatusChangeLogs");

            migrationBuilder.DropTable(
                name: "OrderStatusNotifications");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "UserInvitations");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Baskets");

            migrationBuilder.DropTable(
                name: "BulkMessages");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "CommentTransferJobs");

            migrationBuilder.DropTable(
                name: "TrendyolProducts");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "CompanyModules");

            migrationBuilder.DropTable(
                name: "TrendyolStores");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "ModuleCategories");
        }
    }
}
