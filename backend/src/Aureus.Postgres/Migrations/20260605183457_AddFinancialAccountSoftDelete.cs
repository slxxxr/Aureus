using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aureus.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccountSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_workspace_id_name",
                table: "financial_accounts");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "financial_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "financial_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_workspace_id_name",
                table: "financial_accounts",
                columns: new[] { "workspace_id", "name" },
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_financial_accounts_workspace_id_name",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "financial_accounts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "financial_accounts");

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_workspace_id_name",
                table: "financial_accounts",
                columns: new[] { "workspace_id", "name" },
                unique: true);
        }
    }
}
