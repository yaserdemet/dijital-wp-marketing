using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstOrderCustomersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FirstOrderCustomers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    CustomerExternalId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OrderExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OrderCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EmailPermission = table.Column<bool>(type: "boolean", nullable: false),
                    SmsPermission = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    SmsSent = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppSent = table.Column<bool>(type: "boolean", nullable: false),
                    GiftVoucherCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GiftVoucherAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstOrderCustomers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirstOrderCustomers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FirstOrderCustomers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirstOrderCustomers_CompanyId",
                table: "FirstOrderCustomers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstOrderCustomers_CustomerId",
                table: "FirstOrderCustomers",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirstOrderCustomers");
        }
    }
}
