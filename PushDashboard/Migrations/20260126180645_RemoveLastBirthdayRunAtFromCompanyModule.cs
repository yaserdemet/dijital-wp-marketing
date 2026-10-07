using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PushDashboard.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLastBirthdayRunAtFromCompanyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'CompanyModules'
                        AND column_name = 'LastBirthdayRunAt'
                    ) THEN
                        ALTER TABLE ""CompanyModules"" DROP COLUMN ""LastBirthdayRunAt"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastBirthdayRunAt",
                table: "CompanyModules",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
